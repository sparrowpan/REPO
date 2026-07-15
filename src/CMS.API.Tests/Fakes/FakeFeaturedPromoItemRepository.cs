using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IFeaturedPromoItemRepository"/> so the board API can be exercised end-to-end
/// without SQL Server. Seeds a Monday–Sunday week (3/16–3/22 2026) for two training centers.
/// </summary>
public sealed class FakeFeaturedPromoItemRepository : IFeaturedPromoItemRepository
{
    private readonly List<FeaturedPromoItem> _items = [];
    private int _nextPkid = 1;

    // Promotion pkid -> PromoCode, so CreateAsync can echo the joined PromoCode like the real repo.
    private static readonly Dictionary<int, string> PromoCodes = new()
    {
        [101] = "20251204_SkillTrainAI",
        [102] = "251211_GoogleAI",
        [103] = "20251215_n8n",
    };

    public FakeFeaturedPromoItemRepository()
    {
        // Taipei (center 1): Monday 3/16 has all three slots filled.
        Seed(new DateOnly(2026, 3, 16), 1, 1, 101, "成為能AI協作的程式設計師", "轉職就業養成班");
        Seed(new DateOnly(2026, 3, 16), 1, 2, 102, "Google AI工具一次掌握", "不需技術基礎");
        Seed(new DateOnly(2026, 3, 16), 1, 3, 103, "n8n自動化三部曲", "從自動化新手到企業級架構師");
        // Taipei, Tuesday 3/17, slot 1.
        Seed(new DateOnly(2026, 3, 17), 1, 1, 103, "n8n自動化三部曲", "從自動化新手到企業級架構師");
        // Hsinchu (center 2), Monday 3/16, slot 1 — different center, same week.
        Seed(new DateOnly(2026, 3, 16), 2, 1, 102, "Google AI工具一次掌握", "不需技術基礎");
        // Taipei, a DIFFERENT week (3/23) — must be excluded by the one-week filter.
        Seed(new DateOnly(2026, 3, 23), 1, 1, 101, "成為能AI協作的程式設計師", "轉職就業養成班");
    }

    private void Seed(DateOnly scheduleOn, short center, byte slot, int promotionPkid, string topic, string description)
        => _items.Add(new FeaturedPromoItem
        {
            Pkid = _nextPkid++,
            ScheduleOn = scheduleOn,
            TrainingCenterPkid = center,
            Slot = slot,
            PromotionPkid = promotionPkid,
            PromoCode = PromoCodes.GetValueOrDefault(promotionPkid, string.Empty),
            Topic = topic,
            Description = description,
        });

    public Task<IReadOnlyList<FeaturedPromoItem>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<FeaturedPromoItem>>(Ordered(_items).ToList());

    public Task<IReadOnlyList<FeaturedPromoItem>> QueryAsync(FeaturedPromoItemQuery query, CancellationToken ct = default)
    {
        IEnumerable<FeaturedPromoItem> result = _items;

        if (query.TrainingCenterPkid is { } center)
            result = result.Where(i => i.TrainingCenterPkid == center);
        if (query.ScheduleOnFrom is { } from)
            result = result.Where(i => i.ScheduleOn >= from);
        if (query.ScheduleOnTo is { } to)
            result = result.Where(i => i.ScheduleOn <= to);
        if (query.Slot is { } slot)
            result = result.Where(i => i.Slot == slot);

        return Task.FromResult<IReadOnlyList<FeaturedPromoItem>>(Ordered(result).ToList());
    }

    public Task<FeaturedPromoItem?> GetByPkidAsync(int pkid, CancellationToken ct = default)
        => Task.FromResult(Find(pkid));

    public Task<FeaturedPromoItem> CreateAsync(FeaturedPromoItemRequest request, CancellationToken ct = default)
    {
        var item = new FeaturedPromoItem
        {
            Pkid = _nextPkid++,
            ScheduleOn = request.ScheduleOn,
            TrainingCenterPkid = request.TrainingCenterPkid,
            Slot = request.Slot,
            PromotionPkid = request.PromotionPkid,
            PromoCode = PromoCodes.GetValueOrDefault(request.PromotionPkid, string.Empty),
            Topic = request.Topic,
            Description = request.Description,
        };
        _items.Add(item);
        return Task.FromResult(item);
    }

    public Task<bool> UpdateAsync(FeaturedPromoItemRequest request, CancellationToken ct = default)
    {
        var item = Find(request.Pkid);
        if (item is null)
            return Task.FromResult(false);

        item.ScheduleOn = request.ScheduleOn;
        item.TrainingCenterPkid = request.TrainingCenterPkid;
        item.Slot = request.Slot;
        item.PromotionPkid = request.PromotionPkid;
        item.PromoCode = PromoCodes.GetValueOrDefault(request.PromotionPkid, string.Empty);
        item.Topic = request.Topic;
        item.Description = request.Description;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int pkid, CancellationToken ct = default)
    {
        var item = Find(pkid);
        if (item is null)
            return Task.FromResult(false);

        _items.Remove(item);
        return Task.FromResult(true);
    }

    public Task<bool> MoveToSlotAsync(int pkid, byte targetSlot, CancellationToken ct = default)
    {
        var item = Find(pkid);
        if (item is null || item.Slot == targetSlot)
            return Task.FromResult(false);

        var occupant = _items.FirstOrDefault(i =>
            i.ScheduleOn == item.ScheduleOn && i.TrainingCenterPkid == item.TrainingCenterPkid && i.Slot == targetSlot);

        if (occupant is not null)
            occupant.Slot = item.Slot;
        item.Slot = targetSlot;
        return Task.FromResult(true);
    }

    private static IEnumerable<FeaturedPromoItem> Ordered(IEnumerable<FeaturedPromoItem> items)
        => items.OrderBy(i => i.ScheduleOn).ThenBy(i => i.TrainingCenterPkid).ThenBy(i => i.Slot);

    private FeaturedPromoItem? Find(int pkid) => _items.FirstOrDefault(i => i.Pkid == pkid);
}
