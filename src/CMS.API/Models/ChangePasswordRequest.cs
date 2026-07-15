namespace CMS.API.Models;

/// <summary>
/// Body of <c>POST /api/Auth/change-password</c>. Carries plain-text passwords only — never a hash,
/// in either direction. The target <c>UserId</c> is taken from the authenticated JWT, never from this
/// request, so a user can only ever change their own password.
/// </summary>
public sealed class ChangePasswordRequest
{
    /// <summary>目前密碼 — must hash to the user's stored <c>PasswordHash</c> or the change is rejected.</summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>新密碼 — must satisfy <see cref="Services.PasswordPolicy"/> complexity.</summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>確認新密碼 — must equal <see cref="NewPassword"/>.</summary>
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
