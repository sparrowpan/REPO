using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IPublishStatusRepository
{
    Task<IReadOnlyList<PublishStatus>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PublishStatus>> QueryAsync(PublishStatusQuery query, CancellationToken ct = default);
    Task<PublishStatus?> GetByPkidAsync(byte pkid, CancellationToken ct = default);
    Task<bool> PkidExistsAsync(byte pkid, CancellationToken ct = default);
    Task<PublishStatus> CreateAsync(PublishStatusRequest request, CancellationToken ct = default);
    Task<bool> UpdateAsync(PublishStatusRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(byte pkid, CancellationToken ct = default);
}
