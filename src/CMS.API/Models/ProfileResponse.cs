namespace CMS.API.Models;

/// <summary>
/// The signed-in user's profile after a successful update. Roles are unchanged (they live in the
/// JWT) and are intentionally not part of this response — this endpoint only edits the display name.
/// </summary>
public sealed class ProfileResponse
{
    /// <summary>使用者代碼 — echoed from the JWT; not client-editable.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>使用者名稱 — the saved (trimmed) value.</summary>
    public string UserName { get; set; } = string.Empty;
}
