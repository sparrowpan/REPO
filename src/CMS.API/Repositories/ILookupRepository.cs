using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ILookupRepository
{
    Task<IReadOnlyList<AppUserLookup>> GetAppUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AppRoleLookup>> GetAppRolesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PublishStatusLookup>> GetPublishStatusesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PartnerLookup>> GetPartnersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CourseGroupLookup>> GetCourseGroupsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<JobCategoryLookup>> GetJobCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CertificationLookup>> GetCertificationsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TrainingCenterLookup>> GetTrainingCentersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PromotionLookup>> GetPromotionsAsync(CancellationToken ct = default);
}
