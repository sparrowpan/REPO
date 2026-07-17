namespace CMS.API.Models;

/// <summary>
/// Backend-only credential record read during login. Carries <c>PasswordHash</c> and is used
/// solely to authenticate — it is <b>never</b> serialized to a client (see <see cref="LoginResponse"/>).
/// </summary>
public sealed class LoginCredential
{
    /// <summary>使用者代碼.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>使用者名稱.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>啟用 — login is refused unless this is true.</summary>
    public bool IsActive { get; set; }

    /// <summary>Stored SHA-256 hash (lowercase hex) to compare the supplied password against.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Role ids assigned to this user (via AppUserRole) — become role claims in the JWT.</summary>
    public List<string> RoleIds { get; set; } = [];
}
