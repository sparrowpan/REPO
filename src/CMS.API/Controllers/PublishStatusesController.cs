using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/publish-statuses")]
public class PublishStatusesController(IPublishStatusRepository repository) : ControllerBase
{
    /// <summary>All publishing statuses.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PublishStatus>>> GetAll(CancellationToken ct)
        => Ok(await repository.GetAllAsync(ct));

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<PublishStatus>>> Query([FromBody] PublishStatusQuery query, CancellationToken ct)
        => Ok(await repository.QueryAsync(query, ct));

    /// <summary>Single status by pkid.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PublishStatus>> GetById(byte id, CancellationToken ct)
    {
        var status = await repository.GetByPkidAsync(id, ct);
        return status is null ? NotFound() : Ok(status);
    }

    /// <summary>Create a status (pkid is user-entered).</summary>
    [HttpPost]
    public async Task<ActionResult<PublishStatus>> Create([FromBody] PublishStatusRequest request, CancellationToken ct)
    {
        if (await repository.PkidExistsAsync(request.Pkid, ct))
            return Conflict(new { message = $"主代碼 '{request.Pkid}' 已存在。" });

        var created = await repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Pkid }, created);
    }

    /// <summary>Update a status (pkid in body — immutable key).</summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] PublishStatusRequest request, CancellationToken ct)
    {
        var updated = await repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete a status by pkid.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(byte id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
