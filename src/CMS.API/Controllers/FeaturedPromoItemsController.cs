using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/featured-promo-items")]
public class FeaturedPromoItemsController(IFeaturedPromoItemRepository repository) : ControllerBase
{
    /// <summary>All featured promo items.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FeaturedPromoItem>>> GetAll(CancellationToken ct)
        => Ok(await repository.GetAllAsync(ct));

    /// <summary>Filtered search — one TrainingCenter tab + one Monday–Sunday week.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IReadOnlyList<FeaturedPromoItem>>> Query([FromBody] FeaturedPromoItemQuery query, CancellationToken ct)
        => Ok(await repository.QueryAsync(query, ct));

    /// <summary>Single item by pkid.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<FeaturedPromoItem>> GetById(int id, CancellationToken ct)
    {
        var item = await repository.GetByPkidAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Create an item (pkid auto-generated).</summary>
    [HttpPost]
    public async Task<ActionResult<FeaturedPromoItem>> Create([FromBody] FeaturedPromoItemRequest request, CancellationToken ct)
    {
        var created = await repository.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Pkid }, created);
    }

    /// <summary>Update an item (pkid in body).</summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] FeaturedPromoItemRequest request, CancellationToken ct)
    {
        var updated = await repository.UpdateAsync(request, ct);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Delete an item by pkid.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Move an item to a different slot on the same day (+ / − on the board), swapping occupants.</summary>
    [HttpPost("move")]
    public async Task<IActionResult> Move([FromBody] MoveSlotRequest request, CancellationToken ct)
    {
        var moved = await repository.MoveToSlotAsync(request.Pkid, request.TargetSlot, ct);
        return moved ? NoContent() : NotFound();
    }
}

/// <summary>Body for <c>POST /api/featured-promo-items/move</c>.</summary>
public class MoveSlotRequest
{
    public int Pkid { get; set; }
    public byte TargetSlot { get; set; }
}
