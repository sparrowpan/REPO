using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IAppRoleRepository"/> so the API can be exercised end-to-end
/// (routing, model binding, controller behavior) without a SQL Server instance.
/// </summary>
public sealed class FakeAppRoleRepository : IAppRoleRepository
{
    private readonly List<AppRole> _roles = [];
    private int _nextPkid = 1;

    public FakeAppRoleRepository()
    {
        Seed("Admin", "Administrator", 1, "系統管理員", ["helen", "Jenny_Tsao", "miles"]);
        Seed("User", "User", 100, "一般使用者", ["helen"]);
    }

    private void Seed(string roleId, string roleName, int level, string? description, List<string> userIds)
        => _roles.Add(new AppRole
        {
            Pkid = _nextPkid++,
            RoleId = roleId,
            RoleName = roleName,
            PermissionLevel = level,
            Description = description,
            UserCount = userIds.Count,
            UserIds = userIds,
        });

    public Task<IReadOnlyList<AppRole>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AppRole>>(
            _roles.OrderBy(r => r.RoleId, StringComparer.OrdinalIgnoreCase).ToList());

    public Task<IReadOnlyList<AppRole>> QueryAsync(AppRoleQuery query, CancellationToken ct = default)
    {
        IEnumerable<AppRole> result = _roles;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(r =>
                r.RoleId.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                r.RoleName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                (r.Description?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (query.PermissionLevel is { } level)
            result = result.Where(r => r.PermissionLevel == level);

        return Task.FromResult<IReadOnlyList<AppRole>>(
            result.OrderBy(r => r.RoleId, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public Task<AppRole?> GetByRoleIdAsync(string roleId, CancellationToken ct = default)
        => Task.FromResult(Find(roleId));

    public Task<bool> RoleIdExistsAsync(string roleId, CancellationToken ct = default)
        => Task.FromResult(Find(roleId) is not null);

    public Task<AppRole> CreateAsync(AppRoleRequest request, CancellationToken ct = default)
    {
        var role = new AppRole
        {
            Pkid = _nextPkid++,
            RoleId = request.RoleId,
            RoleName = request.RoleName,
            PermissionLevel = request.PermissionLevel,
            Description = request.Description,
            UserIds = request.UserIds,
            UserCount = request.UserIds.Count,
        };
        _roles.Add(role);
        return Task.FromResult(role);
    }

    public Task<bool> UpdateAsync(AppRoleRequest request, CancellationToken ct = default)
    {
        var role = Find(request.RoleId);
        if (role is null)
            return Task.FromResult(false);

        role.RoleName = request.RoleName;
        role.PermissionLevel = request.PermissionLevel;
        role.Description = request.Description;
        role.UserIds = request.UserIds;
        role.UserCount = request.UserIds.Count;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string roleId, CancellationToken ct = default)
    {
        var role = Find(roleId);
        if (role is null)
            return Task.FromResult(false);

        _roles.Remove(role);
        return Task.FromResult(true);
    }

    private AppRole? Find(string roleId)
        => _roles.FirstOrDefault(r => string.Equals(r.RoleId, roleId, StringComparison.OrdinalIgnoreCase));
}
