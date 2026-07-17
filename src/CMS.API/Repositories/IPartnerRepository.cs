using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IPartnerRepository
{
    Task<IReadOnlyList<Partner>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Partner>> QueryAsync(PartnerQuery query, CancellationToken ct = default);
    Task<Partner?> GetByPkidAsync(short pkid, CancellationToken ct = default);
    Task<Partner> CreateAsync(PartnerRequest request, CancellationToken ct = default);
    Task<bool> UpdateAsync(PartnerRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(short pkid, CancellationToken ct = default);
}
