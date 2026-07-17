using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/course-groups")]
public class CourseGroupsController(ICourseGroupRepository repository) : ControllerBase
{
    /// <summary>All course groups.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourseGroup>>> GetAll(CancellationToken ct)
        => Ok(await repository.GetAllAsync(ct));

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<CourseGroup>>> Query([FromBody] CourseGroupQuery query, CancellationToken ct)
        => Ok(await repository.QueryAsync(query, ct));

    /// <summary>Single course group by pkid.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CourseGroup>> GetById(short id, CancellationToken ct)
    {
        var courseGroup = await repository.GetByPkidAsync(id, ct);
        return courseGroup is null ? NotFound() : Ok(courseGroup);
    }

    /// <summary>Create a course group (pkid auto-generated).</summary>
    [HttpPost]
    public async Task<ActionResult<CourseGroup>> Create([FromBody] CourseGroupRequest request, CancellationToken ct)
    {
        var created = await repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Pkid }, created);
    }

    /// <summary>Update a course group (pkid in body).</summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] CourseGroupRequest request, CancellationToken ct)
    {
        var updated = await repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>
    /// Delete a course group by pkid. A group still assigned to courses cannot be deleted — the FK
    /// refusal comes back as <c>409</c>, not the middleware's generic 500.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(short id, CancellationToken ct)
    {
        try
        {
            var deleted = await repository.DeleteAsync(id, ct);
            return deleted ? NoContent() : NotFound();
        }
        catch (ReferencedRecordException)
        {
            return Conflict(new { message = "此課程群組已被課程使用，無法刪除。" });
        }
    }
}
