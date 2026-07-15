using CMS.API.Models;

namespace CMS.API.Repositories;

/// <summary>Read-only data access for authentication (credentials + JWT signing secret).</summary>
public interface IAuthRepository
{
    /// <summary>
    /// Load the credential + roles for a user by exact <paramref name="userId"/>, or <c>null</c> if
    /// no such user exists. Returns the row regardless of <c>IsActive</c> so the caller can apply a
    /// single generic failure for every rejected case.
    /// </summary>
    Task<LoginCredential?> FindCredentialAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// The JWT signing secret — the <c>symmetricSecurityKey</c> property of the JSON stored in
    /// <c>SysConfig</c> where <c>configKey = 'appConfig'</c>. <c>null</c> if unavailable.
    /// </summary>
    Task<string?> GetSigningSecretAsync(CancellationToken ct = default);

    /// <summary>
    /// Update only <c>UserName</c> for the given <paramref name="userId"/>. Roles, password, and
    /// <c>IsActive</c> are never touched. Returns <c>false</c> when no such user exists.
    /// </summary>
    Task<bool> UpdateUserNameAsync(string userId, string userName, CancellationToken ct = default);

    /// <summary>
    /// Set <c>PasswordHash</c> to <paramref name="passwordHash"/> (a pre-computed SHA-256 hex hash) and
    /// stamp <c>PasswordUpdatedTime</c> to now for the given <paramref name="userId"/>. The caller is
    /// responsible for verifying the current password and complexity first. Returns <c>false</c> when
    /// no such user exists.
    /// </summary>
    Task<bool> UpdatePasswordAsync(string userId, string passwordHash, CancellationToken ct = default);
}
