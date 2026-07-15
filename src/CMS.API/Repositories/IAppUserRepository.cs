using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IAppUserRepository
{
    Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AppUser>> QueryAsync(AppUserQuery query, CancellationToken ct = default);
    Task<AppUser?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<bool> UserIdExistsAsync(string userId, CancellationToken ct = default);
    Task<AppUser> CreateAsync(AppUserRequest request, CancellationToken ct = default);
    Task<bool> UpdateAsync(AppUserRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(string userId, CancellationToken ct = default);

    /// <summary>Reset the user's password to the SysConfig default. Returns false if the user is unknown.</summary>
    Task<bool> ResetPasswordAsync(string userId, CancellationToken ct = default);
}
