using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>Credentials posted to <c>POST /api/Auth/login</c>.</summary>
public sealed class LoginRequest
{
    /// <summary>使用者代碼 — matched exactly against <c>AppUser.UserId</c>.</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Plain-text password; hashed (SHA-256) before comparison. Never stored/logged.</summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}
