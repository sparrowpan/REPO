using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Services;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IAppUserRepository"/> so the API can be exercised end-to-end
/// (routing, model binding, controller behavior) without a SQL Server instance.
/// PasswordHash never leaves the backend, so it is not part of <see cref="AppUser"/>; a reset
/// stamps <see cref="AppUser.PasswordUpdatedTime"/> and records the new hash in a side table
/// (<see cref="GetPasswordHash"/>) so tests can assert it equals <c>SHA-256(DefaultPassword)</c>.
/// </summary>
public sealed class FakeAppUserRepository : IAppUserRepository
{
    /// <summary>
    /// Stands in for the SysConfig <c>appConfig.defaultPassword</c> the real repository reads at
    /// runtime. A reset hashes this value, so tests can assert against <c>SHA-256(DefaultPassword)</c>.
    /// </summary>
    public const string DefaultPassword = "Default#123";

    private readonly List<AppUser> _users = [];

    // Records the last-set PasswordHash per user (AppUser carries no hash — it never leaves the backend).
    private readonly Dictionary<string, string> _passwordHashes = new(StringComparer.OrdinalIgnoreCase);

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

        // Mirror the real repository: PasswordHash becomes SHA-256(default), PasswordUpdatedTime moves to now.
        _passwordHashes[user.UserId] = PasswordHasher.Hash(DefaultPassword);
        user.PasswordUpdatedTime = DateTime.Now;
        return Task.FromResult(true);
    }

    /// <summary>Test hook: the PasswordHash last set for a user, or <c>null</c> if never reset here.</summary>
    public string? GetPasswordHash(string userId)
        => _passwordHashes.TryGetValue(userId, out var hash) ? hash : null;

    private AppUser? Find(string userId)
        => _users.FirstOrDefault(u => string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase));
}
