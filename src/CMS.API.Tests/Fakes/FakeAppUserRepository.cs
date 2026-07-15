using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IAppUserRepository"/> so the API can be exercised end-to-end
/// (routing, model binding, controller behavior) without a SQL Server instance.
/// PasswordHash is not modeled (it never leaves the backend); password operations are
/// simulated by stamping <see cref="AppUser.PasswordUpdatedTime"/>.
/// </summary>
public sealed class FakeAppUserRepository : IAppUserRepository
{
    private readonly List<AppUser> _users = [];
    private int _nextPkid = 1;

    public FakeAppUserRepository()
    {
        Seed("helen", "Helen Wang", true, ["Admin", "User"]);
        Seed("miles", "Miles Sun", false, ["User"]);
    }

    private void Seed(string userId, string userName, bool isActive, List<string> roleIds)
        => _users.Add(new AppUser
        {
            Pkid = _nextPkid++,
            UserId = userId,
            UserName = userName,
            IsActive = isActive,
            PasswordUpdatedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            RoleCount = roleIds.Count,
            RoleIds = roleIds,
        });

    public Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AppUser>>(
            _users.OrderBy(u => u.UserId, StringComparer.OrdinalIgnoreCase).ToList());

    public Task<IReadOnlyList<AppUser>> QueryAsync(AppUserQuery query, CancellationToken ct = default)
    {
        IEnumerable<AppUser> result = _users;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(u =>
                u.UserId.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                u.UserName.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.IsActive is { } active)
            result = result.Where(u => u.IsActive == active);

        return Task.FromResult<IReadOnlyList<AppUser>>(
            result.OrderBy(u => u.UserId, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public Task<AppUser?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(Find(userId));

    public Task<bool> UserIdExistsAsync(string userId, CancellationToken ct = default)
        => Task.FromResult(Find(userId) is not null);

    public Task<AppUser> CreateAsync(AppUserRequest request, CancellationToken ct = default)
    {
        var user = new AppUser
        {
            Pkid = _nextPkid++,
            UserId = request.UserId,
            UserName = request.UserName,
            IsActive = request.IsActive,
            PasswordUpdatedTime = DateTime.Now,
            RoleIds = request.RoleIds,
            RoleCount = request.RoleIds.Count,
        };
        _users.Add(user);
        return Task.FromResult(user);
    }

    public Task<bool> UpdateAsync(AppUserRequest request, CancellationToken ct = default)
    {
        var user = Find(request.UserId);
        if (user is null)
            return Task.FromResult(false);

        // Password fields are intentionally left untouched on update.
        user.UserName = request.UserName;
        user.IsActive = request.IsActive;
        user.RoleIds = request.RoleIds;
        user.RoleCount = request.RoleIds.Count;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string userId, CancellationToken ct = default)
    {
        var user = Find(userId);
        if (user is null)
            return Task.FromResult(false);

        _users.Remove(user);
        return Task.FromResult(true);
    }

    public Task<bool> ResetPasswordAsync(string userId, CancellationToken ct = default)
    {
        var user = Find(userId);
        if (user is null)
            return Task.FromResult(false);

        user.PasswordUpdatedTime = DateTime.Now;
        return Task.FromResult(true);
    }

    private AppUser? Find(string userId)
        => _users.FirstOrDefault(u => string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase));
}
