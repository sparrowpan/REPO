using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Services;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>Dapper-based data access for <see cref="PublishStatus"/> (no EF).</summary>
public sealed class PublishStatusRepository(IDbConnectionFactory connectionFactory, RowAuditWriter audit) : IPublishStatusRepository
{
    private const string TableName = "PublishStatus";

    private const string SelectColumns = """
        SELECT s.pkid, s.Description, s.IsDraft, s.IsPublished, s.IsDiscontinued
        FROM PublishStatus s
        """;

    public async Task<IReadOnlyList<PublishStatus>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY s.pkid ASC";
        var rows = await conn.QueryAsync<PublishStatus>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<PublishStatus>> QueryAsync(PublishStatusQuery query, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            {SelectColumns}
            WHERE (@Keyword IS NULL OR s.Description LIKE '%' + @Keyword + '%')
              AND (@IsDraft IS NULL OR s.IsDraft = @IsDraft)
              AND (@IsPublished IS NULL OR s.IsPublished = @IsPublished)
              AND (@IsDiscontinued IS NULL OR s.IsDiscontinued = @IsDiscontinued)
            ORDER BY s.pkid ASC
            """;
        var parameters = new
        {
            Keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim(),
            query.IsDraft,
            query.IsPublished,
            query.IsDiscontinued,
        };
        var rows = await conn.QueryAsync<PublishStatus>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<PublishStatus?> GetByPkidAsync(byte pkid, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} WHERE s.pkid = @Pkid";
        return await conn.QuerySingleOrDefaultAsync<PublishStatus>(
            new CommandDefinition(sql, new { Pkid = pkid }, cancellationToken: ct));
    }

    public async Task<bool> PkidExistsAsync(byte pkid, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM PublishStatus WHERE pkid = @Pkid", new { Pkid = pkid }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<PublishStatus> CreateAsync(PublishStatusRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        // pkid is user-entered (tinyint, not identity) — insert it explicitly, no SCOPE_IDENTITY().
        await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO PublishStatus (pkid, Description, IsDraft, IsPublished, IsDiscontinued)
            VALUES (@Pkid, @Description, @IsDraft, @IsPublished, @IsDiscontinued);
            """, request, tx, cancellationToken: ct));

        var created = new PublishStatus
        {
            Pkid = request.Pkid,
            Description = request.Description,
            IsDraft = request.IsDraft,
            IsPublished = request.IsPublished,
            IsDiscontinued = request.IsDiscontinued,
        };

        await audit.LogInsert(TableName, created, conn, tx, ct);
        tx.Commit();
        return created;
    }

    public async Task<bool> UpdateAsync(PublishStatusRequest request, CancellationToken ct = default)
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
            UPDATE PublishStatus
            SET Description = @Description,
                IsDraft = @IsDraft,
                IsPublished = @IsPublished,
                IsDiscontinued = @IsDiscontinued
            WHERE pkid = @Pkid;
            """, request, tx, cancellationToken: ct));

        var after = await LoadForAuditAsync(conn, tx, request.Pkid, ct);
        await audit.LogUpdate(TableName, before, after!, conn, tx, ct);
        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(byte pkid, CancellationToken ct = default)
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
                "DELETE FROM PublishStatus WHERE pkid = @Pkid", new { Pkid = pkid }, tx, cancellationToken: ct));
        }
        catch (Exception ex) when (ReferencedRecordException.IsForeignKeyViolation(ex))
        {
            // Course rows still point at this status. Surface it as a domain failure the controller
            // can turn into a 409 — unhandled, it would reach the middleware as a generic 500.
            throw new ReferencedRecordException(TableName, ex);
        }

        await audit.LogDelete(TableName, existing, conn, tx, ct);
        tx.Commit();
        return true;
    }

    /// <summary>Load a status's own columns on the given connection/transaction, for audit before/after snapshots.</summary>
    private static async Task<PublishStatus?> LoadForAuditAsync(IDbConnection conn, IDbTransaction tx, byte pkid, CancellationToken ct)
        => await conn.QuerySingleOrDefaultAsync<PublishStatus>(
            new CommandDefinition($"{SelectColumns} WHERE s.pkid = @Pkid", new { Pkid = pkid }, tx, cancellationToken: ct));
}
