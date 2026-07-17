using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Services;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>Dapper-based data access for <see cref="CourseGroup"/> (no EF).</summary>
public sealed class CourseGroupRepository(IDbConnectionFactory connectionFactory, RowAuditWriter audit) : ICourseGroupRepository
{
    private const string TableName = "CourseGroup";

    private const string SelectColumns = """
        SELECT g.pkid, g.Description
        FROM CourseGroup g
        """;

    public async Task<IReadOnlyList<CourseGroup>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY g.pkid ASC";
        var rows = await conn.QueryAsync<CourseGroup>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CourseGroup>> QueryAsync(CourseGroupQuery query, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            {SelectColumns}
            WHERE (@Keyword IS NULL OR g.Description LIKE '%' + @Keyword + '%')
            ORDER BY g.pkid ASC
            """;
        var parameters = new
        {
            Keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim(),
        };
        var rows = await conn.QueryAsync<CourseGroup>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<CourseGroup?> GetByPkidAsync(short pkid, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} WHERE g.pkid = @Pkid";
        return await conn.QuerySingleOrDefaultAsync<CourseGroup>(
            new CommandDefinition(sql, new { Pkid = pkid }, cancellationToken: ct));
    }

    public async Task<CourseGroup> CreateAsync(CourseGroupRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var pkid = await conn.ExecuteScalarAsync<short>(new CommandDefinition("""
            INSERT INTO CourseGroup (Description)
            VALUES (@Description);
            SELECT CAST(SCOPE_IDENTITY() AS smallint);
            """, request, tx, cancellationToken: ct));

        var created = new CourseGroup
        {
            Pkid = pkid,
            Description = request.Description,
        };

        await audit.LogInsert(TableName, created, conn, tx, ct);
        tx.Commit();
        return created;
    }

    public async Task<bool> UpdateAsync(CourseGroupRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var before = await LoadForAuditAsync(conn, tx, request.Pkid, ct);
        if (before is null)
        {
            tx.Rollback();
            return false;
        }

        await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE CourseGroup
            SET Description = @Description
            WHERE pkid = @Pkid;
            """, request, tx, cancellationToken: ct));

        var after = await LoadForAuditAsync(conn, tx, request.Pkid, ct);
        await audit.LogUpdate(TableName, before, after!, conn, tx, ct);
        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(short pkid, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var existing = await LoadForAuditAsync(conn, tx, pkid, ct);
        if (existing is null)
        {
            tx.Rollback();
            return false;
        }

        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM CourseGroup WHERE pkid = @Pkid", new { Pkid = pkid }, tx, cancellationToken: ct));
        }
        catch (Exception ex) when (ReferencedRecordException.IsForeignKeyViolation(ex))
        {
            // Course rows still point at this group. Surface it as a domain failure the controller
            // can turn into a 409 — unhandled, it would reach the middleware as a generic 500.
            throw new ReferencedRecordException(TableName, ex);
        }

        await audit.LogDelete(TableName, existing, conn, tx, ct);
        tx.Commit();
        return true;
    }

    /// <summary>Load a group's own columns on the given connection/transaction, for audit before/after snapshots.</summary>
    private static async Task<CourseGroup?> LoadForAuditAsync(IDbConnection conn, IDbTransaction tx, short pkid, CancellationToken ct)
        => await conn.QuerySingleOrDefaultAsync<CourseGroup>(
            new CommandDefinition($"{SelectColumns} WHERE g.pkid = @Pkid", new { Pkid = pkid }, tx, cancellationToken: ct));
}
