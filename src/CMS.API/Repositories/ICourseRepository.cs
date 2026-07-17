using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ICourseRepository
{
    Task<IReadOnlyList<Course>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Course>> QueryAsync(CourseQuery query, CancellationToken ct = default);
    Task<Course?> GetByPkidAsync(int pkid, CancellationToken ct = default);
    Task<Course> CreateAsync(CourseRequest request, CancellationToken ct = default);
    Task<bool> UpdateAsync(CourseRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int pkid, CancellationToken ct = default);
}
