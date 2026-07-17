namespace CMS.API.Models;

/// <summary>Slim lookup item for AppRole, used as the n-n target of AppUser.</summary>
public class AppRoleLookup
{
    /// <summary>RoleId — value stored in the AppUserRole junction.</summary>
    public string RoleId { get; set; } = string.Empty;

    /// <summary>角色名稱.</summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>Display label — "RoleName (RoleId)".</summary>
    public string Label => $"{RoleName} ({RoleId})";
}
