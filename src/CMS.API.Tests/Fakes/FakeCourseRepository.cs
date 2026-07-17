using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="ICourseRepository"/> so the API can be exercised end-to-end
/// (routing, model binding, controller behavior) without a SQL Server instance.
/// </summary>
public sealed class FakeCourseRepository : ICourseRepository
{
    private readonly List<Course> _courses = [];
    private int _nextPkid = 1;

    public FakeCourseRepository()
    {
        Seed("AZ-900 基礎課程", "AZ900", "PROD-AZ900", 1, 1, "微軟", 10, "初階", 20, canRepeat: true,
            jobCategoryPkids: [1, 2], certificationPkids: [1]);
        Seed("PMP 專案管理", "PMP", "PROD-PMP", 2, 2, "PMI", null, "—", 21, canRepeat: false,
            jobCategoryPkids: [], certificationPkids: []);
    }

    private void Seed(string title, string courseId, string prodCourseId, int displayOrder, short partnerPkid,
        string partnerName, short? courseGroupPkid, string courseGroupDesc, byte publishStatusPkid,
        bool canRepeat, short[] jobCategoryPkids, int[] certificationPkids)
    {
        var course = new Course
        {
            Pkid = _nextPkid++,
            Title = title,
            CourseId = courseId,
            ProdCourseId = prodCourseId,
            FriendlyUrl = courseId.ToLowerInvariant(),
            DisplayOrder = displayOrder,
            PartnerPkid = partnerPkid,
            CourseGroupPkid = courseGroupPkid,
            PublishStatusPkid = publishStatusPkid,
            ScheduleOn = new DateOnly(2026, 1, 1),
            ScheduleOff = new DateOnly(2030, 12, 31),
            Hour = 8,
            ListPrice = 12000,
            LearningCredit = 3.5m,
            CanRepeat = canRepeat,
            Partner = new PartnerLookup { Pkid = partnerPkid, Name = partnerName },
            CourseGroup = courseGroupPkid is null ? null : new CourseGroupLookup { Pkid = courseGroupPkid.Value, Description = courseGroupDesc },
            PublishStatus = new PublishStatusLookup { Pkid = publishStatusPkid, Description = "已上架" },
            JobCategoryCount = jobCategoryPkids.Length,
            CertificationCount = certificationPkids.Length,
            JobCategoryPkids = jobCategoryPkids.ToList(),
            CertificationPkids = certificationPkids.ToList(),
        };
        _courses.Add(course);
    }

    public Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Course>>(
            _courses.OrderBy(c => c.DisplayOrder).ThenByDescending(c => c.Pkid).ToList());

    public Task<IReadOnlyList<Course>> QueryAsync(CourseQuery query, CancellationToken ct = default)
    {
        IEnumerable<Course> result = _courses;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(c =>
                c.Title.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                (c.OfficialTitle?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false) ||
                c.CourseId.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                c.ProdCourseId.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                c.FriendlyUrl.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.PartnerPkid is { } partner)
            result = result.Where(c => c.PartnerPkid == partner);
        if (query.CourseGroupPkid is { } group)
            result = result.Where(c => c.CourseGroupPkid == group);
        if (query.PublishStatusPkid is { } status)
            result = result.Where(c => c.PublishStatusPkid == status);
        if (query.CanRepeat is { } canRepeat)
            result = result.Where(c => c.CanRepeat == canRepeat);
        if (query.ScheduleOnFrom is { } onFrom)
            result = result.Where(c => c.ScheduleOn >= onFrom);
        if (query.ScheduleOnTo is { } onTo)
            result = result.Where(c => c.ScheduleOn <= onTo);
        if (query.ScheduleOffFrom is { } offFrom)
            result = result.Where(c => c.ScheduleOff >= offFrom);
        if (query.ScheduleOffTo is { } offTo)
            result = result.Where(c => c.ScheduleOff <= offTo);

        return Task.FromResult<IReadOnlyList<Course>>(
            result.OrderBy(c => c.DisplayOrder).ThenByDescending(c => c.Pkid).ToList());
    }

    public Task<Course?> GetByPkidAsync(int pkid, CancellationToken ct = default)
        => Task.FromResult(Find(pkid));

    public Task<Course> CreateAsync(CourseRequest request, CancellationToken ct = default)
    {
        var course = FromRequest(request);
        course.Pkid = _nextPkid++;
        _courses.Add(course);
        return Task.FromResult(course);
    }

    public Task<bool> UpdateAsync(CourseRequest request, CancellationToken ct = default)
    {
        var existing = Find(request.Pkid);
        if (existing is null)
            return Task.FromResult(false);

        var updated = FromRequest(request);
        updated.Pkid = request.Pkid;
        _courses[_courses.IndexOf(existing)] = updated;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int pkid, CancellationToken ct = default)
    {
        var course = Find(pkid);
        if (course is null)
            return Task.FromResult(false);

        _courses.Remove(course);
        return Task.FromResult(true);
    }

    private static Course FromRequest(CourseRequest r) => new()
    {
        Title = r.Title,
        OfficialTitle = r.OfficialTitle,
        CourseId = r.CourseId,
        ProdCourseId = r.ProdCourseId,
        FriendlyUrl = r.FriendlyUrl,
        DisplayOrder = r.DisplayOrder,
        PartnerPkid = r.PartnerPkid,
        CourseGroupPkid = r.CourseGroupPkid,
        PublishStatusPkid = r.PublishStatusPkid,
        ScheduleOn = r.ScheduleOn,
        ScheduleOff = r.ScheduleOff,
        Hour = r.Hour,
        ListPrice = r.ListPrice,
        LearningCredit = r.LearningCredit,
        Material = r.Material,
        Objective = r.Objective,
        Target = r.Target,
        Prerequisites = r.Prerequisites,
        Outline = r.Outline,
        TowardCertOrExam = r.TowardCertOrExam,
        Note = r.Note,
        OtherInfo = r.OtherInfo,
        CanRepeat = r.CanRepeat,
        JobCategoryCount = r.JobCategoryPkids.Count,
        CertificationCount = r.CertificationPkids.Count,
        JobCategoryPkids = r.JobCategoryPkids.ToList(),
        CertificationPkids = r.CertificationPkids.ToList(),
    };

    private Course? Find(int pkid) => _courses.FirstOrDefault(c => c.Pkid == pkid);
}
