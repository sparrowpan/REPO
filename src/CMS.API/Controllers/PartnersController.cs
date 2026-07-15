using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/partners")]
public class PartnersController(IPartnerRepository repository) : ControllerBase
{
    /// <summary>All partners.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Partner>>> GetAll(CancellationToken ct)
        => Ok(await repository.GetAllAsync(ct));

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<Partner>>> Query([FromBody] PartnerQuery query, CancellationToken ct)
        => Ok(await repository.QueryAsync(query, ct));

    /// <summary>Single partner by pkid.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Partner>> GetById(short id, CancellationToken ct)
    {
        var partner = await repository.GetByPkidAsync(id, ct);
        return partner is null ? NotFound() : Ok(partner);
    }

    /// <summary>Create a partner (pkid auto-generated).</summary>
    [HttpPost]
    public async Task<ActionResult<Partner>> Create([FromBody] PartnerRequest request, CancellationToken ct)
    {
        var created = await repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Pkid }, created);
    }

    /// <summary>Update a partner (pkid in body).</summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] PartnerRequest request, CancellationToken ct)
    {
        var updated = await repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete a partner by pkid.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(short id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
