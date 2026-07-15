namespace CMS.API.Models;

/// <summary>Search DTO for filtering <see cref="AppUser"/> records.</summary>
public class AppUserQuery
{
    /// <summary>LIKE match on UserId, UserName.</summary>
    public string? Keyword { get; set; }

    /// <summary>Exact match on IsActive (tri-state: null = all, true = active, false = inactive).</summary>
    public bool? IsActive { get; set; }
}
