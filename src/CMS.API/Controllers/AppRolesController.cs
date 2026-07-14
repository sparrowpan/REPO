using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/approles")]
public class AppRolesController(IAppRoleRepository repository) : ControllerBase
{
    /// <summary>All roles.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppRole>>> GetAll(CancellationToken ct)
        => Ok(await repository.GetAllAsync(ct));

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<AppRole>>> Query([FromBody] AppRoleQuery query, CancellationToken ct)
        => Ok(await repository.QueryAsync(query, ct));

    /// <summary>Single role by RoleId (string PK).</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppRole>> GetById(string id, CancellationToken ct)
    {
        var role = await repository.GetByRoleIdAsync(id, ct);
        return role is null ? NotFound() : Ok(role);
    }

    /// <summary>Create a role.</summary>
    [HttpPost]
    public async Task<ActionResult<AppRole>> Create([FromBody] AppRoleRequest request, CancellationToken ct)
    {
        if (await repository.RoleIdExistsAsync(request.RoleId, ct))
            return Conflict(new { message = $"角色代碼 '{request.RoleId}' 已存在。" });

        var created = await repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.RoleId }, created);
    }

    /// <summary>Update a role (RoleId in body — immutable business key).</summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] AppRoleRequest request, CancellationToken ct)
    {
        var updated = await repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete a role by RoleId.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
