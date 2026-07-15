using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Services;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>Dapper-based data access for <see cref="Partner"/> (no EF).</summary>
public sealed class PartnerRepository(IDbConnectionFactory connectionFactory, RowAuditWriter audit) : IPartnerRepository
{
    private const string TableName = "Partner";

    private const string SelectColumns = """
        SELECT p.pkid, p.Name, p.AppKey, p.NameOnPartnerMenu, p.NameOnCourseDetailPage,
               p.DisplayOrder, p.ImageFilename
        FROM Partner p
        """;

    public async Task<IReadOnlyList<Partner>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY p.DisplayOrder ASC";
        var rows = await conn.QueryAsync<Partner>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<Partner>> QueryAsync(PartnerQuery query, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            {SelectColumns}
            WHERE (@Keyword IS NULL
                   OR p.Name LIKE '%' + @Keyword + '%'
                   OR p.AppKey LIKE '%' + @Keyword + '%'
                   OR p.NameOnPartnerMenu LIKE '%' + @Keyword + '%'
                   OR p.NameOnCourseDetailPage LIKE '%' + @Keyword + '%')
            ORDER BY p.DisplayOrder ASC
            """;
        var parameters = new
        {
            Keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim(),
        };
        var rows = await conn.QueryAsync<Partner>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Partner?> GetByPkidAsync(short pkid, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} WHERE p.pkid = @Pkid";
        return await conn.QuerySingleOrDefaultAsync<Partner>(
            new CommandDefinition(sql, new { Pkid = pkid }, cancellationToken: ct));
    }

    public async Task<Partner> CreateAsync(PartnerRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var pkid = await conn.ExecuteScalarAsync<short>(new CommandDefinition("""
            INSERT INTO Partner (Name, AppKey, NameOnPartnerMenu, NameOnCourseDetailPage, DisplayOrder, ImageFilename)
            VALUES (@Name, @AppKey, @NameOnPartnerMenu, @NameOnCourseDetailPage, @DisplayOrder, @ImageFilename);
            SELECT CAST(SCOPE_IDENTITY() AS smallint);
            """, request, tx, cancellationToken: ct));

        var created = new Partner
        {
            Pkid = pkid,
            Name = request.Name,
            AppKey = request.AppKey,
            NameOnPartnerMenu = request.NameOnPartnerMenu,
            NameOnCourseDetailPage = request.NameOnCourseDetailPage,
            DisplayOrder = request.DisplayOrder,
            ImageFilename = request.ImageFilename,
        };

        await audit.LogInsert(TableName, created, conn, tx, ct);
        tx.Commit();
        return created;
    }

    public async Task<bool> UpdateAsync(PartnerRequest request, CancellationToken ct = default)
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
            UPDATE Partner
            SET Name = @Name,
                AppKey = @AppKey,
                NameOnPartnerMenu = @NameOnPartnerMenu,
                NameOnCourseDetailPage = @NameOnCourseDetailPage,
                DisplayOrder = @DisplayOrder,
                ImageFilename = @ImageFilename
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

        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Partner WHERE pkid = @Pkid", new { Pkid = pkid }, tx, cancellationToken: ct));

        await audit.LogDelete(TableName, existing, conn, tx, ct);
        tx.Commit();
        return true;
    }

    /// <summary>Load a partner's own columns on the given connection/transaction, for audit before/after snapshots.</summary>
    private static async Task<Partner?> LoadForAuditAsync(IDbConnection conn, IDbTransaction tx, short pkid, CancellationToken ct)
        => await conn.QuerySingleOrDefaultAsync<Partner>(
            new CommandDefinition($"{SelectColumns} WHERE p.pkid = @Pkid", new { Pkid = pkid }, tx, cancellationToken: ct));
}
