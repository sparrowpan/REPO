using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Services;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;

namespace CMS.API.Tests;

/// <summary>
/// Integration tests proving <see cref="PublishStatusRepository"/> (a retrofitted repository) writes the
/// right <see cref="RowAudit"/> row on each path — and none when the change fails. PublishStatus is used
/// because its SQL is provider-portable (user-entered PK, no <c>SCOPE_IDENTITY()</c>), so the REAL repository
/// runs unchanged against an in-memory SQLite database that also holds a RowAudit table.
/// </summary>
public sealed class PublishStatusRepositoryAuditTests : IDisposable
{
    // A shared-cache in-memory DB, unique per test instance. The keep-alive connection keeps it alive while
    // the repository opens (and disposes) its own connections against the same shared database.
    private readonly string _connectionString = $"Data Source=RowAudit-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;
    private readonly PublishStatusRepository _repo;

    public PublishStatusRepositoryAuditTests()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
        CreateSchema(_keepAlive);

        var audit = new RowAuditWriter(new HttpContextAccessor { HttpContext = null }); // no user -> "system"
        _repo = new PublishStatusRepository(new SqliteConnectionFactory(_connectionString), audit);
    }

    public void Dispose() => _keepAlive.Dispose();

    // --- Insert -------------------------------------------------------------

    [Fact]
    public async Task Create_WritesInsertAuditRow_WithFirstStringColumn()
    {
        await _repo.CreateAsync(Draft(5), CancellationToken.None);

        var row = Assert.Single(AuditRows());
        Assert.Equal("PublishStatus", row.TableName);
        Assert.Equal("Insert", row.ActionType);
        Assert.Equal("5", row.PrimaryKeyValues);
        Assert.Equal("草稿", row.ActionDesc); // Description is the first string column
        Assert.Equal("system", row.UserName);
    }

    // --- Update -------------------------------------------------------------

    [Fact]
    public async Task Update_WritesUpdateAuditRow_ListingExactlyTheChangedColumns()
    {
        await _repo.CreateAsync(Draft(5), CancellationToken.None);

        // Change Description + IsDraft + IsPublished; leave IsDiscontinued unchanged.
        await _repo.UpdateAsync(new PublishStatusRequest
        {
            Pkid = 5,
            Description = "已發佈",
            IsDraft = false,
            IsPublished = true,
            IsDiscontinued = false,
        }, CancellationToken.None);

        var rows = AuditRows();
        Assert.Equal(2, rows.Count);
        var update = rows[1];
        Assert.Equal("Update", update.ActionType);
        Assert.Equal("5", update.PrimaryKeyValues);
        Assert.Equal("Description, IsDraft, IsPublished", update.ActionDesc);
    }

    [Fact]
    public async Task Update_WithNoRealChange_WritesNoAuditRow()
    {
        await _repo.CreateAsync(Draft(5), CancellationToken.None);

        var ok = await _repo.UpdateAsync(Draft(5), CancellationToken.None); // identical values

        Assert.True(ok);                        // the row exists, so the update "succeeds"
        Assert.Single(AuditRows());             // ...but nothing changed, so only the original Insert row remains
    }

    // --- Delete -------------------------------------------------------------

    [Fact]
    public async Task Delete_WritesDeleteAuditRow_WithFirstStringColumn()
    {
        await _repo.CreateAsync(Draft(5), CancellationToken.None);

        await _repo.DeleteAsync(5, CancellationToken.None);

        var rows = AuditRows();
        Assert.Equal(2, rows.Count);
        var delete = rows[1];
        Assert.Equal("Delete", delete.ActionType);
        Assert.Equal("5", delete.PrimaryKeyValues);
        Assert.Equal("草稿", delete.ActionDesc);
    }

    // --- Failed change leaves no audit row ----------------------------------

    [Fact]
    public async Task Update_MissingRow_WritesNoAuditRow()
    {
        var ok = await _repo.UpdateAsync(Draft(99), CancellationToken.None); // pkid 99 does not exist

        Assert.False(ok);
        Assert.Empty(AuditRows());
    }

    [Fact]
    public async Task Delete_MissingRow_WritesNoAuditRow()
    {
        var ok = await _repo.DeleteAsync(99, CancellationToken.None);

        Assert.False(ok);
        Assert.Empty(AuditRows());
    }

    [Fact]
    public async Task Create_ThatFails_RollsBackAndWritesNoAuditRow()
    {
        await _repo.CreateAsync(Draft(5), CancellationToken.None);

        // Re-inserting pkid 5 violates the primary key: the INSERT throws, the transaction rolls back,
        // and the audit row that would have been written is rolled back with it.
        await Assert.ThrowsAnyAsync<SqliteException>(() => _repo.CreateAsync(Draft(5), CancellationToken.None));

        Assert.Single(AuditRows()); // only the first successful Insert audit row survives
    }

    // --- Helpers ------------------------------------------------------------

    private static PublishStatusRequest Draft(byte pkid) => new()
    {
        Pkid = pkid,
        Description = "草稿",
        IsDraft = true,
        IsPublished = false,
        IsDiscontinued = false,
    };

    private List<AuditRow> AuditRows() => _keepAlive.Query<AuditRow>(
        "SELECT TableName, UserName, PrimaryKeyValues, ActionType, ActionDesc FROM RowAudit ORDER BY pkid").AsList();

    private static void CreateSchema(IDbConnection conn)
    {
        conn.Execute("""
            CREATE TABLE PublishStatus (
                pkid           INTEGER PRIMARY KEY,
                Description    TEXT    NOT NULL,
                IsDraft        INTEGER NOT NULL,
                IsPublished    INTEGER NOT NULL,
                IsDiscontinued INTEGER NOT NULL
            );

            CREATE TABLE RowAudit (
                pkid             INTEGER PRIMARY KEY AUTOINCREMENT,
                TableName        TEXT NOT NULL,
                UserName         TEXT NOT NULL,
                PrimaryKeyValues TEXT NOT NULL,
                ActionType       TEXT NOT NULL,
                ActionDesc       TEXT NULL,
                [DateTime]       TEXT NOT NULL
            );
            """);
    }

    private sealed record AuditRow(string TableName, string UserName, string PrimaryKeyValues, string ActionType, string? ActionDesc);

    /// <summary>Hands the repository fresh connections to the shared in-memory SQLite database.</summary>
    private sealed class SqliteConnectionFactory(string connectionString) : IDbConnectionFactory
    {
        public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken ct = default)
        {
            var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync(ct);
            return conn;
        }
    }
}
