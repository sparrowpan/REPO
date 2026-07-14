using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IAppRoleRepository
{
    Task<IReadOnlyList<AppRole>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AppRole>> QueryAsync(AppRoleQuery query, CancellationToken ct = default);
    Task<AppRole?> GetByRoleIdAsync(string roleId, CancellationToken ct = default);
    Task<bool> RoleIdExistsAsync(string roleId, CancellationToken ct = default);
    Task<AppRole> CreateAsync(AppRoleRequest request, CancellationToken ct = default);
    Task<bool> UpdateAsync(AppRoleRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(string roleId, CancellationToken ct = default);
}
