using System.Security.Claims;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

/// <summary>
/// Authentication endpoints. <c>login</c> is public (<c>[AllowAnonymous]</c>); every other action
/// (e.g. updating your own profile) is protected by the global authenticated-user policy.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthRepository repository, IJwtTokenService tokenService) : ControllerBase
{
    /// <summary>
    /// Authenticate a user and return a profile with a 24-hour JWT access token.
    /// Any failed check (unknown user, inactive account, wrong password) yields the same
    /// generic 401 so the response never reveals which part failed.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var credential = await repository.FindCredentialAsync(request.UserId, ct);

        if (credential is null ||
            !credential.IsActive ||
            !PasswordHasher.Verify(request.Password, credential.PasswordHash))
        {
            return Unauthorized(new { message = "invalid credentials" });
        }

        var signingSecret = await repository.GetSigningSecretAsync(ct);
        if (string.IsNullOrEmpty(signingSecret))
        {
            // The signing key is missing/misconfigured in SysConfig — a server problem, not a bad credential.
            return Problem(
                title: "Authentication is not configured.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var accessToken = tokenService.CreateToken(credential, signingSecret);

        return Ok(new LoginResponse
        {
            UserId = credential.UserId,
            UserName = credential.UserName,
            AccessToken = accessToken,
        });
    }

    /// <summary>
    /// Update the signed-in user's display name. The target user is the authenticated caller —
    /// taken from the JWT <c>userId</c> claim — so any <c>UserId</c> in the request body is ignored
    /// and a user can only rename themselves. Only <c>UserName</c> changes; roles and every other
    /// field are left untouched. Requires authentication (this action has no <c>[AllowAnonymous]</c>).
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile(
        [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue("userId");
        if (string.IsNullOrEmpty(userId))
        {
            // Authenticated but the token carries no userId claim — treat as unauthenticated.
            return Unauthorized();
        }

        var userName = request.UserName?.Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            return BadRequest(new { message = "使用者名稱為必填。" });
        }

        var updated = await repository.UpdateUserNameAsync(userId, userName, ct);
        if (!updated)
        {
            return NotFound(new { message = "找不到使用者。" });
        }

        return Ok(new ProfileResponse { UserId = userId, UserName = userName });
    }

    /// <summary>
    /// Change the signed-in user's password. The target user is the authenticated caller (JWT
    /// <c>userId</c> claim), so a caller can only ever change their own password — no id is read from
    /// the body. Steps, in order: verify the current password against the stored hash; enforce
    /// new-password complexity (<see cref="PasswordPolicy"/>); require new == confirm. Only on success
    /// is <c>PasswordHash</c> re-hashed and <c>PasswordUpdatedTime</c> stamped. No password hash ever
    /// crosses this boundary — the request carries plain text and only the new hash is persisted.
    /// Requires authentication (no <c>[AllowAnonymous]</c>).
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue("userId");
        if (string.IsNullOrEmpty(userId))
        {
            // Authenticated but the token carries no userId claim — treat as unauthenticated.
            return Unauthorized();
        }

        var credential = await repository.FindCredentialAsync(userId, ct);
        if (credential is null)
        {
            return NotFound(new { message = "找不到使用者。" });
        }

        // 1. The current password must hash to the stored hash — otherwise nothing changes.
        if (!PasswordHasher.Verify(request.CurrentPassword, credential.PasswordHash))
        {
            return BadRequest(new { message = "目前密碼不正確。Current password is incorrect." });
        }

        // 2. New password must satisfy the complexity policy.
        if (!PasswordPolicy.IsCompliant(request.NewPassword))
        {
            return BadRequest(new { message = PasswordPolicy.ComplexityMessage });
        }

        // 3. New and confirmation must match.
        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            return BadRequest(new { message = "新密碼與確認密碼不一致。New password and confirmation do not match." });
        }

        // 4. Persist the new hash and stamp the update time.
        var updated = await repository.UpdatePasswordAsync(userId, PasswordHasher.Hash(request.NewPassword), ct);
        if (!updated)
        {
            return NotFound(new { message = "找不到使用者。" });
        }

        return Ok(new { message = "密碼已更新。Password changed." });
    }
}
