namespace CMS.API.Models;

/// <summary>
/// Slim Promotion2 lookup item. The board's Edit form resolves a typed PromoCode to
/// <see cref="Pkid"/> and defaults Topic / Description from the matched promotion.
/// </summary>
public class PromotionLookup
{
    /// <summary>主代碼 — value stored in FeaturedPromoItem.Promotion_pkid.</summary>
    public int Pkid { get; set; }

    /// <summary>活動代碼 — unique; what the user types to look up the promotion.</summary>
    public string PromoCode { get; set; } = string.Empty;

    /// <summary>標題 — used to pre-fill the item's Topic.</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>描述 — used to pre-fill the item's Description.</summary>
    public string Description { get; set; } = string.Empty;
}
