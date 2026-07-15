using System.Data;
using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Services;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>Dapper-based data access for <see cref="AppRole"/> (no EF).</summary>
public sealed class AppRoleRepository(IDbConnectionFactory connectionFactory, RowAuditWriter audit) : IAppRoleRepository
{
    private const string TableName = "AppRole";

    private const string SelectColumns = """
        SELECT r.pkid, r.RoleId, r.RoleName, r.PermissionLevel, r.Description,
               (SELECT COUNT(*) FROM AppUserRole ur WHERE ur.RoleId = r.RoleId) AS UserCount
        FROM AppRole r
        """;

    // Own-table columns only (no UserCount subquery) — used for audit before/after snapshots so the
    // changed-column list stays limited to real AppRole columns and ignores n-n membership churn.
    private const string AuditSelectColumns = """
        SELECT r.pkid, r.RoleId, r.RoleName, r.PermissionLevel, r.Description
        FROM AppRole r
        """;

    public async Task<IReadOnlyList<AppRole>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY r.RoleId ASC";
        var rows = await conn.QueryAsync<AppRole>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<AppRole>> QueryAsync(AppRoleQuery query, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            {SelectColumns}
            WHERE (@Keyword IS NULL
                   OR r.RoleId LIKE '%' + @Keyword + '%'
                   OR r.RoleName LIKE '%' + @Keyword + '%'
                   OR r.Description LIKE '%' + @Keyword + '%')
              AND (@PermissionLevel IS NULL OR r.PermissionLevel = @PermissionLevel)
            ORDER BY r.RoleId ASC
            """;
        var parameters = new
        {
            Keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim(),
            query.PermissionLevel,
        };
        var rows = await conn.QueryAsync<AppRole>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<AppRole?> GetByRoleIdAsync(string roleId, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            {SelectColumns} WHERE r.RoleId = @RoleId;
            SELECT UserId FROM AppUserRole WHERE RoleId = @RoleId ORDER BY UserId;
            """;
        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: ct));
        var role = await multi.ReadSingleOrDefaultAsync<AppRole>();
        if (role is null)
            return null;
        role.UserIds = (await multi.ReadAsync<string>()).ToList();
        return role;
    }

    public async Task<bool> RoleIdExistsAsync(string roleId, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM AppRole WHERE RoleId = @RoleId", new { RoleId = roleId }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<AppRole> CreateAsync(AppRoleRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var pkid = await conn.ExecuteScalarAsync<int>(new CommandDefinition("""
            INSERT INTO AppRole (RoleId, RoleName, PermissionLevel, Description)
            VALUES (@RoleId, @RoleName, @PermissionLevel, @Description);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """, request, tx, cancellationToken: ct));

        await SyncUsersAsync(conn, tx, request.RoleId, request.UserIds, ct);

        var created = new AppRole
        {
            Pkid = pkid,
            RoleId = request.RoleId,
            RoleName = request.RoleName,
            PermissionLevel = request.PermissionLevel,
            Description = request.Description,
            UserCount = request.UserIds.Count,
            UserIds = request.UserIds,
        };

        await audit.LogInsert(TableName, created, conn, tx, ct);
        tx.Commit();
        return created;
    }

    public async Task<bool> UpdateAsync(AppRoleRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var before = await LoadForAuditAsync(conn, tx, request.RoleId, ct);
        if (before is null)
        {
            tx.Rollback();
            return false;
        }

        var affected = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE AppRole
            SET RoleName = @RoleName,
                PermissionLevel = @PermissionLevel,
                Description = @Description
            WHERE RoleId = @RoleId;
            """, request, tx, cancellationToken: ct));

        if (affected == 0)
        {
            tx.Rollback();
            return false;
        }

        await SyncUsersAsync(conn, tx, request.RoleId, request.UserIds, ct);

        var after = await LoadForAuditAsync(conn, tx, request.RoleId, ct);
        await audit.LogUpdate(TableName, before, after!, conn, tx, ct);
        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(string roleId, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var existing = await LoadForAuditAsync(conn, tx, roleId, ct);
        if (existing is null)
        {
            tx.Rollback();
            return false;
        }

        // Remove junction rows first to satisfy the FK constraint.
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AppUserRole WHERE RoleId = @RoleId", new { RoleId = roleId }, tx, cancellationToken: ct));
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AppRole WHERE RoleId = @RoleId", new { RoleId = roleId }, tx, cancellationToken: ct));

        await audit.LogDelete(TableName, existing, conn, tx, ct);
        tx.Commit();
        return true;
    }

    /// <summary>Load a role's own columns on the given connection/transaction, for audit before/after snapshots.</summary>
    private static async Task<AppRole?> LoadForAuditAsync(IDbConnection conn, IDbTransaction tx, string roleId, CancellationToken ct)
        => await conn.QuerySingleOrDefaultAsync<AppRole>(
            new CommandDefinition($"{AuditSelectColumns} WHERE r.RoleId = @RoleId", new { RoleId = roleId }, tx, cancellationToken: ct));

    /// <summary>n-n sync: delete-then-reinsert the AppUserRole rows for a role.</summary>
    private static async Task SyncUsersAsync(
        IDbConnection conn, IDbTransaction tx, string roleId, List<string> userIds, CancellationToken ct)
    {
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AppUserRole WHERE RoleId = @RoleId", new { RoleId = roleId }, tx, cancellationToken: ct));

        var distinct = userIds.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
        if (distinct.Count == 0)
            return;

        await conn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId)",
            distinct.Select(u => new { UserId = u, RoleId = roleId }), tx, cancellationToken: ct));
    }
}
