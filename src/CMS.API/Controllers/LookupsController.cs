using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/lookups")]
public class LookupsController(ILookupRepository repository) : ControllerBase
{
    /// <summary>Slim AppUser list for the AppRole n-n user select.</summary>
    [HttpGet("appusers")]
    public async Task<ActionResult<IReadOnlyList<AppUserLookup>>> GetAppUsers(CancellationToken ct)
        => Ok(await repository.GetAppUsersAsync(ct));

    /// <summary>Slim AppRole list for the AppUser n-n role select.</summary>
    [HttpGet("approles")]
    public async Task<ActionResult<IReadOnlyList<AppRoleLookup>>> GetAppRoles(CancellationToken ct)
        => Ok(await repository.GetAppRolesAsync(ct));

    /// <summary>Slim PublishStatus list for FK selects (Course / Promotion2).</summary>
    [HttpGet("publish-statuses")]
    public async Task<ActionResult<IReadOnlyList<PublishStatusLookup>>> GetPublishStatuses(CancellationToken ct)
        => Ok(await repository.GetPublishStatusesAsync(ct));

    /// <summary>Slim Partner list for FK selects (Course / Certification).</summary>
    [HttpGet("partners")]
    public async Task<ActionResult<IReadOnlyList<PartnerLookup>>> GetPartners(CancellationToken ct)
        => Ok(await repository.GetPartnersAsync(ct));

    /// <summary>Slim CourseGroup list for FK selects (Course).</summary>
    [HttpGet("course-groups")]
    public async Task<ActionResult<IReadOnlyList<CourseGroupLookup>>> GetCourseGroups(CancellationToken ct)
        => Ok(await repository.GetCourseGroupsAsync(ct));

    /// <summary>Slim JobCategory list for the Course n-n multiselect.</summary>
    [HttpGet("job-categories")]
    public async Task<ActionResult<IReadOnlyList<JobCategoryLookup>>> GetJobCategories(CancellationToken ct)
        => Ok(await repository.GetJobCategoriesAsync(ct));

    /// <summary>Slim Certification list for the Course n-n multiselect.</summary>
    [HttpGet("certifications")]
    public async Task<ActionResult<IReadOnlyList<CertificationLookup>>> GetCertifications(CancellationToken ct)
        => Ok(await repository.GetCertificationsAsync(ct));
}
