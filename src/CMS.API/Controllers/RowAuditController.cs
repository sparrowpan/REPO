using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

/// <summary>
/// Read-only audit history for a single record. Cross-cutting: any detail/form page can fetch the trail
/// for the record it shows via <c>GET /api/rowaudit?tableName=Course&amp;pkid=123</c>, newest first.
/// </summary>
[ApiController]
[Route("api/rowaudit")]
public class RowAuditController(IRowAuditRepository repository) : ControllerBase
{
    /// <summary>Audit rows for one record, filtered by table name + surrogate pkid, newest first.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RowAuditEntry>>> Get(
        [FromQuery] string? tableName, [FromQuery] int pkid, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return BadRequest(new { message = "tableName is required." });

        var entries = await repository.GetForRecordAsync(tableName.Trim(), pkid, ct);
        return Ok(entries);
    }
}
