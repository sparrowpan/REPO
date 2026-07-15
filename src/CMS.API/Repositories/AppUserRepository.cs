using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>Dapper-based data access for <see cref="AppUser"/> (no EF).</summary>
/// <remarks>
/// PasswordHash is never read into the model nor accepted from the client. On create it is derived
/// from the SysConfig default password; it changes only via <see cref="ResetPasswordAsync"/>.
/// </remarks>
public sealed class AppUserRepository(IDbConnectionFactory connectionFactory) : IAppUserRepository
{
    /// <summary>Used when SysConfig has no appConfig row / no defaultPassword property.</summary>
    private const string FallbackDefaultPassword = "Password123!";

    private const string SelectColumns = """
        SELECT u.pkid, u.UserId, u.UserName, u.IsActive, u.PasswordUpdatedTime,
               (SELECT COUNT(*) FROM AppUserRole ur WHERE ur.UserId = u.UserId) AS RoleCount
        FROM AppUser u
        """;

    public async Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"{SelectColumns} ORDER BY u.UserId ASC";
        var rows = await conn.QueryAsync<AppUser>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<AppUser>> QueryAsync(AppUserQuery query, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            {SelectColumns}
            WHERE (@Keyword IS NULL
                   OR u.UserId LIKE '%' + @Keyword + '%'
                   OR u.UserName LIKE '%' + @Keyword + '%')
              AND (@IsActive IS NULL OR u.IsActive = @IsActive)
            ORDER BY u.UserId ASC
            """;
        var parameters = new
        {
            Keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim(),
            query.IsActive,
        };
        var rows = await conn.QueryAsync<AppUser>(new CommandDefinition(sql, parameters, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<AppUser?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            {SelectColumns} WHERE u.UserId = @UserId;
            SELECT RoleId FROM AppUserRole WHERE UserId = @UserId ORDER BY RoleId;
            """;
        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        var user = await multi.ReadSingleOrDefaultAsync<AppUser>();
        if (user is null)
            return null;
        user.RoleIds = (await multi.ReadAsync<string>()).ToList();
        return user;
    }

    public async Task<bool> UserIdExistsAsync(string userId, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM AppUser WHERE UserId = @UserId", new { UserId = userId }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<AppUser> CreateAsync(AppUserRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        var passwordHash = HashPassword(await GetDefaultPasswordAsync(conn, tx, ct));

        var pkid = await conn.ExecuteScalarAsync<int>(new CommandDefinition("""
            INSERT INTO AppUser (UserId, UserName, IsActive, PasswordHash, PasswordUpdatedTime)
            VALUES (@UserId, @UserName, @IsActive, @PasswordHash, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """,
            new { request.UserId, request.UserName, request.IsActive, PasswordHash = passwordHash },
            tx, cancellationToken: ct));

        await SyncRolesAsync(conn, tx, request.UserId, request.RoleIds, ct);
        tx.Commit();

        return new AppUser
        {
            Pkid = pkid,
            UserId = request.UserId,
            UserName = request.UserName,
            IsActive = request.IsActive,
            PasswordUpdatedTime = DateTime.Now,
            RoleCount = request.RoleIds.Count,
            RoleIds = request.RoleIds,
        };
    }

    public async Task<bool> UpdateAsync(AppUserRequest request, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        // PasswordHash / PasswordUpdatedTime are intentionally left untouched here.
        var affected = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE AppUser
            SET UserName = @UserName,
                IsActive = @IsActive
            WHERE UserId = @UserId;
            """, request, tx, cancellationToken: ct));

        if (affected == 0)
        {
            tx.Rollback();
            return false;
        }

        await SyncRolesAsync(conn, tx, request.UserId, request.RoleIds, ct);
        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(string userId, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = conn.BeginTransaction();

        // Remove junction rows first to satisfy the FK constraint.
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AppUserRole WHERE UserId = @UserId", new { UserId = userId }, tx, cancellationToken: ct));
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AppUser WHERE UserId = @UserId", new { UserId = userId }, tx, cancellationToken: ct));

        tx.Commit();
        return affected > 0;
    }

    public async Task<bool> ResetPasswordAsync(string userId, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var passwordHash = HashPassword(await GetDefaultPasswordAsync(conn, null, ct));

        var affected = await conn.ExecuteAsync(new CommandDefinition("""
            UPDATE AppUser
            SET PasswordHash = @PasswordHash, PasswordUpdatedTime = GETDATE()
            WHERE UserId = @UserId;
            """, new { UserId = userId, PasswordHash = passwordHash }, cancellationToken: ct));
        return affected > 0;
    }

    /// <summary>n-n sync: delete-then-reinsert the AppUserRole rows for a user.</summary>
    private static async Task SyncRolesAsync(
        IDbConnection conn, IDbTransaction tx, string userId, List<string> roleIds, CancellationToken ct)
    {
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AppUserRole WHERE UserId = @UserId", new { UserId = userId }, tx, cancellationToken: ct));

        var distinct = roleIds.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();
        if (distinct.Count == 0)
            return;

        await conn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO AppUserRole (UserId, RoleId) VALUES (@UserId, @RoleId)",
            distinct.Select(r => new { UserId = userId, RoleId = r }), tx, cancellationToken: ct));
    }

    /// <summary>Read the default password from SysConfig (configKey='appConfig' → JSON defaultPassword).</summary>
    private static async Task<string> GetDefaultPasswordAsync(
        IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        var configValue = await conn.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT configValue FROM SysConfig WHERE configKey = 'appConfig'", null, tx, cancellationToken: ct));

        if (string.IsNullOrWhiteSpace(configValue))
            return FallbackDefaultPassword;

        try
        {
            using var doc = JsonDocument.Parse(configValue);
            if (doc.RootElement.TryGetProperty("defaultPassword", out var prop) &&
                prop.ValueKind == JsonValueKind.String)
            {
                var value = prop.GetString();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
        }
        catch (JsonException)
        {
            // Malformed config — fall back to the constant default.
        }

        return FallbackDefaultPassword;
    }

    /// <summary>SHA-256 of the UTF-8 password bytes, as a lowercase hex string (fits nvarchar(800)).</summary>
    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexStringLower(bytes);
    }
}
