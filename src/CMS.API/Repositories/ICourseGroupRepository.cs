using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ICourseGroupRepository
{
    Task<IReadOnlyList<CourseGroup>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CourseGroup>> QueryAsync(CourseGroupQuery query, CancellationToken ct = default);
    Task<CourseGroup?> GetByPkidAsync(short pkid, CancellationToken ct = default);
    Task<CourseGroup> CreateAsync(CourseGroupRequest request, CancellationToken ct = default);
    Task<bool> UpdateAsync(CourseGroupRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(short pkid, CancellationToken ct = default);
}
