namespace CMS.API.Models;

/// <summary>Profile returned on successful login. Never carries <c>PasswordHash</c>.</summary>
public sealed class LoginResponse
{
    /// <summary>使用者代碼.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>使用者名稱.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Signed JWT access token (valid for 24 hours).</summary>
    public string AccessToken { get; set; } = string.Empty;
}
