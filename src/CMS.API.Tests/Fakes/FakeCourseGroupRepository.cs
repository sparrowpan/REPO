using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="ICourseGroupRepository"/> so the API can be exercised end-to-end
/// (routing, model binding, controller behavior) without a SQL Server instance.
/// </summary>
public sealed class FakeCourseGroupRepository : ICourseGroupRepository
{
    private readonly List<CourseGroup> _groups = [];
    private short _nextPkid = 1;

    public FakeCourseGroupRepository()
    {
        Seed("資訊技術");
        Seed("商業管理");
    }

    private void Seed(string description)
        => _groups.Add(new CourseGroup
        {
            Pkid = _nextPkid++,
            Description = description,
        });

    public Task<IReadOnlyList<CourseGroup>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CourseGroup>>(_groups.OrderBy(g => g.Pkid).ToList());

    public Task<IReadOnlyList<CourseGroup>> QueryAsync(CourseGroupQuery query, CancellationToken ct = default)
    {
        IEnumerable<CourseGroup> result = _groups;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(g =>
                g.Description.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<CourseGroup>>(result.OrderBy(g => g.Pkid).ToList());
    }

    public Task<CourseGroup?> GetByPkidAsync(short pkid, CancellationToken ct = default)
        => Task.FromResult(Find(pkid));

    public Task<CourseGroup> CreateAsync(CourseGroupRequest request, CancellationToken ct = default)
    {
        var group = new CourseGroup
        {
            Pkid = _nextPkid++,
            Description = request.Description,
        };
        _groups.Add(group);
        return Task.FromResult(group);
    }

    public Task<bool> UpdateAsync(CourseGroupRequest request, CancellationToken ct = default)
    {
        var group = Find(request.Pkid);
        if (group is null)
            return Task.FromResult(false);

        group.Description = request.Description;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(short pkid, CancellationToken ct = default)
    {
        var group = Find(pkid);
        if (group is null)
            return Task.FromResult(false);

        _groups.Remove(group);
        return Task.FromResult(true);
    }

    private CourseGroup? Find(short pkid) => _groups.FirstOrDefault(g => g.Pkid == pkid);
}
