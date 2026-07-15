using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Services;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IAuthRepository"/> so login can be exercised end-to-end without SQL Server.
/// Seeds an active user (<c>helen</c>) and an inactive one (<c>miles</c>), both with password
/// <see cref="Password"/>, and supplies a fixed signing secret in place of the SysConfig lookup.
/// </summary>
public sealed class FakeAuthRepository : IAuthRepository
{
    /// <summary>Plain-text password shared by both seeded users.</summary>
    public const string Password = "secret123";

    /// <summary>Stands in for the <c>symmetricSecurityKey</c> from SysConfig (long enough for HMAC-SHA256).</summary>
    public const string SigningSecret = "test-signing-secret-key-of-sufficient-length-0123456789";

    /// <summary>Fixed seed value so tests can assert <c>PasswordUpdatedTime</c> actually moves forward.</summary>
    public static readonly DateTime InitialPasswordUpdatedTime = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    // Tracks the last password-update time per user (LoginCredential carries no such field).
    private readonly Dictionary<string, DateTime> _passwordUpdatedTimes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["helen"] = InitialPasswordUpdatedTime,
        ["miles"] = InitialPasswordUpdatedTime,
    };

    private readonly List<LoginCredential> _credentials =
    [
        new()
        {
            UserId = "helen",
            UserName = "Helen Wang",
            IsActive = true,
            PasswordHash = PasswordHasher.Hash(Password),
            RoleIds = ["Admin", "User"],
        },
        new()
        {
            UserId = "miles",
            UserName = "Miles Sun",
            IsActive = false,
            PasswordHash = PasswordHasher.Hash(Password),
            RoleIds = ["User"],
        },
    ];

    public Task<LoginCredential?> FindCredentialAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(_credentials.FirstOrDefault(
            c => string.Equals(c.UserId, userId, StringComparison.OrdinalIgnoreCase)));

    public Task<string?> GetSigningSecretAsync(CancellationToken ct = default)
        => Task.FromResult<string?>(SigningSecret);

    public Task<bool> UpdateUserNameAsync(string userId, string userName, CancellationToken ct = default)
    {
        var cred = _credentials.FirstOrDefault(
            c => string.Equals(c.UserId, userId, StringComparison.OrdinalIgnoreCase));
        if (cred is null)
            return Task.FromResult(false);

        cred.UserName = userName;
        return Task.FromResult(true);
    }

    public Task<bool> UpdatePasswordAsync(string userId, string passwordHash, CancellationToken ct = default)
    {
        var cred = _credentials.FirstOrDefault(
            c => string.Equals(c.UserId, userId, StringComparison.OrdinalIgnoreCase));
        if (cred is null)
            return Task.FromResult(false);

        cred.PasswordHash = passwordHash;
        _passwordUpdatedTimes[userId] = DateTime.Now;
        return Task.FromResult(true);
    }

    /// <summary>Test hook: the stored SHA-256 hash for a user, or <c>null</c> if unknown.</summary>
    public string? GetPasswordHash(string userId)
        => _credentials.FirstOrDefault(
            c => string.Equals(c.UserId, userId, StringComparison.OrdinalIgnoreCase))?.PasswordHash;

    /// <summary>Test hook: the last recorded <c>PasswordUpdatedTime</c> for a user.</summary>
    public DateTime? GetPasswordUpdatedTime(string userId)
        => _passwordUpdatedTimes.TryGetValue(userId, out var t) ? t : null;
}
