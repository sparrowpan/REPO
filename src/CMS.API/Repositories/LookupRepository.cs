using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>Dapper-based slim lookup queries used to populate FK / n-n select options.</summary>
public sealed class LookupRepository(IDbConnectionFactory connectionFactory) : ILookupRepository
{
    public async Task<IReadOnlyList<AppUserLookup>> GetAppUsersAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<AppUserLookup>(new CommandDefinition("""
            SELECT UserId, UserName
            FROM AppUser
            ORDER BY UserName
            """, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<AppRoleLookup>> GetAppRolesAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<AppRoleLookup>(new CommandDefinition("""
            SELECT RoleId, RoleName
            FROM AppRole
            ORDER BY RoleId
            """, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<PublishStatusLookup>> GetPublishStatusesAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<PublishStatusLookup>(new CommandDefinition("""
            SELECT pkid, Description
            FROM PublishStatus
            ORDER BY pkid
            """, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<PartnerLookup>> GetPartnersAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<PartnerLookup>(new CommandDefinition("""
            SELECT pkid, Name
            FROM Partner
            ORDER BY DisplayOrder
            """, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CourseGroupLookup>> GetCourseGroupsAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<CourseGroupLookup>(new CommandDefinition("""
            SELECT pkid, Description
            FROM CourseGroup
            ORDER BY pkid
            """, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<JobCategoryLookup>> GetJobCategoriesAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<JobCategoryLookup>(new CommandDefinition("""
            SELECT pkid, Description
            FROM JobCategory
            ORDER BY Description
            """, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CertificationLookup>> GetCertificationsAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        // Certification.Title is nchar(100) — RTRIM the trailing padding.
        var rows = await conn.QueryAsync<CertificationLookup>(new CommandDefinition("""
            SELECT pkid, RTRIM(Title) AS Title
            FROM Certification
            ORDER BY pkid
            """, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<TrainingCenterLookup>> GetTrainingCentersAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<TrainingCenterLookup>(new CommandDefinition("""
            SELECT pkid, Name
            FROM TrainingCenter
            ORDER BY DisplayOrder
            """, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<PromotionLookup>> GetPromotionsAsync(CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<PromotionLookup>(new CommandDefinition("""
            SELECT pkid, PromoCode, Topic, Description
            FROM Promotion2
            ORDER BY PromoCode
            """, cancellationToken: ct));
        return rows.ToList();
    }
}
