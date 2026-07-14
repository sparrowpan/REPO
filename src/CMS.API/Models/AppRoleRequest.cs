using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>Write DTO for creating / updating an <see cref="AppRole"/>.</summary>
public class AppRoleRequest
{
    /// <summary>主代碼 — surrogate key (present on update, ignored on insert).</summary>
    public int Pkid { get; set; }

    /// <summary>角色代碼 — immutable business key; required on create.</summary>
    [Required]
    [MaxLength(200)]
    public string RoleId { get; set; } = string.Empty;

    /// <summary>角色名稱.</summary>
    [Required]
    [MaxLength(200)]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>權限等級.</summary>
    public int PermissionLevel { get; set; } = 100;

    /// <summary>描述.</summary>
    [MaxLength(400)]
    public string? Description { get; set; }

    /// <summary>Assigned user ids (n-n via AppUserRole).</summary>
    public List<string> UserIds { get; set; } = [];
}
