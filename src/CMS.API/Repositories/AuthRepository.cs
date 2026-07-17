using System.Data;
using System.Text.Json;
using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>Dapper-based authentication data access (no EF). Reads credentials and the JWT secret.</summary>
public sealed class AuthRepository(IDbConnectionFactory connectionFactory) : IAuthRepository
{
    public async Task<LoginCredential?> FindCredentialAsync(string userId, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = """
            SELECT u.UserId, u.UserName, u.IsActive, u.PasswordHash
            FROM AppUser u WHERE u.UserId = @UserId;
            SELECT RoleId FROM AppUserRole WHERE UserId = @UserId ORDER BY RoleId;
            """;
        using var multi = await conn.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));

        var cred = await multi.ReadSingleOrDefaultAsync<LoginCredential>();
        if (cred is null)
            return null;

        cred.RoleIds = (await multi.ReadAsync<string>()).ToList();
        return cred;
    }

    public async Task<string?> GetSigningSecretAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var configValue = await conn.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT configValue FROM SysConfig WHERE configKey = 'appConfig'", cancellationToken: ct));

        if (string.IsNullOrWhiteSpace(configValue))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(configValue);
            if (doc.RootElement.TryGetProperty("symmetricSecurityKey", out var prop) &&
                prop.ValueKind == JsonValueKind.String)
            {
                var value = prop.GetString();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
        }
        catch (JsonException)
        {
            // Malformed appConfig — treated as "no secret available".
        }

        return null;
    }

    public async Task<bool> UpdateUserNameAsync(string userId, string userName, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE AppUser SET UserName = @UserName WHERE UserId = @UserId",
            new { UserId = userId, UserName = userName }, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> UpdatePasswordAsync(string userId, string passwordHash, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE AppUser SET PasswordHash = @PasswordHash, PasswordUpdatedTime = GETDATE() WHERE UserId = @UserId",
            new { UserId = userId, PasswordHash = passwordHash }, cancellationToken: ct));
        return affected > 0;
    }
}
