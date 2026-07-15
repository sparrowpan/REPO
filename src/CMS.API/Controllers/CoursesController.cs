using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(ICourseRepository repository) : ControllerBase
{
    /// <summary>All courses (with FK nav labels + n-n counts).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Course>>> GetAll(CancellationToken ct)
        => Ok(await repository.GetAllAsync(ct));

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<Course>>> Query([FromBody] CourseQuery query, CancellationToken ct)
        => Ok(await repository.QueryAsync(query, ct));

    /// <summary>Single course by pkid (includes FK nav objects + n-n pkid / label lists).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Course>> GetById(int id, CancellationToken ct)
    {
        var course = await repository.GetByPkidAsync(id, ct);
        return course is null ? NotFound() : Ok(course);
    }

    /// <summary>Create a course (pkid auto-generated).</summary>
    [HttpPost]
    public async Task<ActionResult<Course>> Create([FromBody] CourseRequest request, CancellationToken ct)
    {
        var created = await repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Pkid }, created);
    }

    /// <summary>Update a course (pkid in body).</summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] CourseRequest request, CancellationToken ct)
    {
        var updated = await repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete a course by pkid.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
