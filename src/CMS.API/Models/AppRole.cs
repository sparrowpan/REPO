namespace CMS.API.Models;

/// <summary>Response model for an application role (角色).</summary>
public class AppRole
{
    /// <summary>主代碼 — surrogate identity key.</summary>
    public int Pkid { get; set; }

    /// <summary>角色代碼 — business/primary key.</summary>
    public string RoleId { get; set; } = string.Empty;

    /// <summary>角色名稱.</summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>權限等級.</summary>
    public int PermissionLevel { get; set; }

    /// <summary>描述.</summary>
    public string? Description { get; set; }

    /// <summary>使用者數 — count of users assigned to this role (n-n via AppUserRole).</summary>
    public int UserCount { get; set; }

    /// <summary>Assigned user ids (populated on GET by id).</summary>
    public List<string> UserIds { get; set; } = [];
}
