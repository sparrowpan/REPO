using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="ILookupRepository"/>. Only the lookups exercised by tests
/// (TrainingCenter tabs + Promotion PromoCode) are seeded; the rest return empty lists.
/// </summary>
public sealed class FakeLookupRepository : ILookupRepository
{
    public Task<IReadOnlyList<AppUserLookup>> GetAppUsersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AppUserLookup>>([]);

    public Task<IReadOnlyList<AppRoleLookup>> GetAppRolesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AppRoleLookup>>([]);

    public Task<IReadOnlyList<PublishStatusLookup>> GetPublishStatusesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PublishStatusLookup>>([]);

    public Task<IReadOnlyList<PartnerLookup>> GetPartnersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PartnerLookup>>([]);

    public Task<IReadOnlyList<CourseGroupLookup>> GetCourseGroupsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CourseGroupLookup>>([]);

    public Task<IReadOnlyList<JobCategoryLookup>> GetJobCategoriesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<JobCategoryLookup>>([]);

    public Task<IReadOnlyList<CertificationLookup>> GetCertificationsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CertificationLookup>>([]);

    public Task<IReadOnlyList<TrainingCenterLookup>> GetTrainingCentersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TrainingCenterLookup>>(
        [
            new() { Pkid = 1, Name = "台北" },
            new() { Pkid = 2, Name = "新竹" },
            new() { Pkid = 3, Name = "台中" },
            new() { Pkid = 4, Name = "高雄" },
            new() { Pkid = 5, Name = "線上研討會" },
        ]);

    public Task<IReadOnlyList<PromotionLookup>> GetPromotionsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PromotionLookup>>(
        [
            new() { Pkid = 101, PromoCode = "20251204_SkillTrainAI", Topic = "成為能AI協作的程式設計師", Description = "轉職就業養成班" },
            new() { Pkid = 102, PromoCode = "251211_GoogleAI", Topic = "Google AI工具一次掌握", Description = "不需技術基礎" },
            new() { Pkid = 103, PromoCode = "20251215_n8n", Topic = "n8n自動化三部曲", Description = "從自動化新手到企業級架構師" },
        ]);
}
