using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IFeaturedPromoItemRepository
{
    Task<IReadOnlyList<FeaturedPromoItem>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FeaturedPromoItem>> QueryAsync(FeaturedPromoItemQuery query, CancellationToken ct = default);
    Task<FeaturedPromoItem?> GetByPkidAsync(int pkid, CancellationToken ct = default);
    Task<FeaturedPromoItem> CreateAsync(FeaturedPromoItemRequest request, CancellationToken ct = default);
    Task<bool> UpdateAsync(FeaturedPromoItemRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int pkid, CancellationToken ct = default);

    /// <summary>
    /// Move an item to a different Slot on the same (ScheduleOn, TrainingCenter) day, swapping
    /// with whatever occupies the target slot. Honors the unique (date, center, slot) index.
    /// </summary>
    Task<bool> MoveToSlotAsync(int pkid, byte targetSlot, CancellationToken ct = default);
}
