using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>Write DTO for creating / updating an <see cref="AppUser"/>.</summary>
/// <remarks>
/// <c>PasswordHash</c> is deliberately not part of this DTO — the password is defaulted from
/// <c>SysConfig</c> on create and only changed via the dedicated reset-password endpoint.
/// </remarks>
public class AppUserRequest
{
    /// <summary>主代碼 — surrogate key (present on update, ignored on insert).</summary>
    public int Pkid { get; set; }

    /// <summary>使用者代碼 — immutable business key; required on create.</summary>
    [Required]
    [MaxLength(200)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>使用者名稱.</summary>
    [Required]
    [MaxLength(200)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>啟用.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Assigned role ids (n-n via AppUserRole).</summary>
    public List<string> RoleIds { get; set; } = [];
}
