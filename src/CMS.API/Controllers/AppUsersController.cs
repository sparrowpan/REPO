using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/appusers")]
public class AppUsersController(IAppUserRepository repository) : ControllerBase
{
    /// <summary>All users.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppUser>>> GetAll(CancellationToken ct)
        => Ok(await repository.GetAllAsync(ct));

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<AppUser>>> Query([FromBody] AppUserQuery query, CancellationToken ct)
        => Ok(await repository.QueryAsync(query, ct));

    /// <summary>Single user by UserId (string PK).</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppUser>> GetById(string id, CancellationToken ct)
    {
        var user = await repository.GetByUserIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>Create a user (password defaulted from SysConfig).</summary>
    [HttpPost]
    public async Task<ActionResult<AppUser>> Create([FromBody] AppUserRequest request, CancellationToken ct)
    {
        if (await repository.UserIdExistsAsync(request.UserId, ct))
            return Conflict(new { message = $"使用者代碼 '{request.UserId}' 已存在。" });

        var created = await repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.UserId }, created);
    }

    /// <summary>Update a user (UserId in body — immutable business key). Never changes the password.</summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] AppUserRequest request, CancellationToken ct)
    {
        var updated = await repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete a user by UserId.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// Reset a user's password to the SysConfig default. Restricted to the <c>Admin</c> role
    /// (enforced server-side via the role claim); a non-Admin caller gets 403 Forbidden. No
    /// password or hash is returned — success is a bare 204.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, CancellationToken ct)
    {
        var reset = await repository.ResetPasswordAsync(id, ct);
        return reset ? NoContent() : NotFound();
    }
}
