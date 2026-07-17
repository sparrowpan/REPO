using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Body of <c>PUT /api/Auth/profile</c>. Carries only the editable field — the target
/// <c>UserId</c> is taken from the authenticated JWT, never from this request, so a user can
/// only ever rename themselves.
/// </summary>
public sealed class UpdateProfileRequest
{
    /// <summary>使用者名稱 — required; trimmed server-side before saving.</summary>
    [Required]
    [MaxLength(200)]
    public string UserName { get; set; } = string.Empty;
}
