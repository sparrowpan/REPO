using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>Write DTO for creating / updating a featured promo item.</summary>
public class FeaturedPromoItemRequest
{
    public int Pkid { get; set; } // present on update, ignored on insert (IDENTITY)

    public DateOnly ScheduleOn { get; set; }

    public short TrainingCenterPkid { get; set; }

    /// <summary>版位 — 1 / 2 / 3.</summary>
    [Range(1, 3)]
    public byte Slot { get; set; }

    /// <summary>活動 FK — resolved from the entered PromoCode on the client.</summary>
    public int PromotionPkid { get; set; }

    [Required, MaxLength(100)]
    public string Topic { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;
}
