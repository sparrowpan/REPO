namespace CMS.API.Models;

/// <summary>Response model for an application login account (使用者).</summary>
/// <remarks>
/// <c>PasswordHash</c> is intentionally absent — it is a backend-only column and is never
/// sent to the frontend. See <see cref="AppUserRequest"/> and the reset-password endpoint.
/// </remarks>
public class AppUser
{
    /// <summary>主代碼 — surrogate identity key.</summary>
    public int Pkid { get; set; }

    /// <summary>使用者代碼 — business/primary key.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>使用者名稱.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>啟用.</summary>
    public bool IsActive { get; set; }

    /// <summary>密碼更新時間 — read-only; set by the backend on create / password reset.</summary>
    public DateTime? PasswordUpdatedTime { get; set; }

    /// <summary>角色數 — count of roles assigned to this user (n-n via AppUserRole).</summary>
    public int RoleCount { get; set; }

    /// <summary>Assigned role ids (populated on GET by id).</summary>
    public List<string> RoleIds { get; set; } = [];
}
