namespace CMS.API.Models;

/// <summary>
/// Response model for a featured promo item (上稿作業) — one scheduled promo occupying a
/// (ScheduleOn, TrainingCenter, Slot) cell on the weekly home-page board. Carries the
/// joined <see cref="PromoCode"/> from Promotion2 for display.
/// </summary>
public class FeaturedPromoItem
{
    /// <summary>主代碼 — surrogate identity key.</summary>
    public int Pkid { get; set; }

    /// <summary>上稿日期 — the day this promo appears on the board.</summary>
    public DateOnly ScheduleOn { get; set; }

    /// <summary>訓練中心 FK (TrainingCenter.pkid).</summary>
    public short TrainingCenterPkid { get; set; }

    /// <summary>版位 — 1 / 2 / 3.</summary>
    public byte Slot { get; set; }

    /// <summary>活動 FK (Promotion2.pkid).</summary>
    public int PromotionPkid { get; set; }

    /// <summary>活動代碼 — joined from Promotion2 for display / editing.</summary>
    public string PromoCode { get; set; } = string.Empty;

    /// <summary>標題.</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>描述.</summary>
    public string Description { get; set; } = string.Empty;
}
