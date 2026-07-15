using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>Dapper-based data access for <see cref="PublishStatus"/> (no EF).</summary>
public sealed class PublishStatusRepository(IDbConnectionFactory connectionFactory) : IPublishStatusRepository
{
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

        // pkid is user-entered (tinyint, not identity) — insert it explicitly, no SCOPE_IDENTITY().
        await conn.ExecuteAsync(new CommandDefinition("""
            INSERT INTO PublishStatus (pkid, Description, IsDraft, IsPublished, IsDiscontinued)
            VALUES (@Pkid, @Description, @IsDraft, @IsPublished, @IsDiscontinued);
            """, request, cancellationToken: ct));

        return new PublishStatus
        {
            Pkid = request.Pkid,
            Description = request.Description,
            IsDraft = request.IsDraft,
            IsPublished = request.IsPublished,
            IsDiscontinued = request.IsDiscontinued,
        };
    }

    public async Task<bool> UpdateAsync(PublishStatusRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE PublishStatus
            SET Description = @Description,
                IsDraft = @IsDraft,
                IsPublished = @IsPublished,
                IsDiscontinued = @IsDiscontinued
            WHERE pkid = @Pkid;
            """, request, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(byte pkid, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM PublishStatus WHERE pkid = @Pkid", new { Pkid = pkid }, cancellationToken: ct));
        return affected > 0;
    }
}
