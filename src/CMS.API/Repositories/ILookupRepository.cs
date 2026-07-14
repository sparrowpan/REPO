using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ILookupRepository
{
    Task<IReadOnlyList<AppUserLookup>> GetAppUsersAsync(CancellationToken ct = default);
}
