namespace CMS.API.Models;

/// <summary>Slim lookup item for AppUser, used as the n-n target of AppRole.</summary>
public class AppUserLookup
{
    /// <summary>UserId — value stored in the AppUserRole junction.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>使用者名稱.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Display label — "UserName (UserId)".</summary>
    public string Label => $"{UserName} ({UserId})";
}
