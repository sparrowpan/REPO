namespace CMS.API.Models;

/// <summary>Search DTO for filtering <see cref="AppRole"/> records.</summary>
public class AppRoleQuery
{
    /// <summary>LIKE match on RoleId, RoleName, Description.</summary>
    public string? Keyword { get; set; }

    /// <summary>Exact match on PermissionLevel (optional).</summary>
    public int? PermissionLevel { get; set; }
}
