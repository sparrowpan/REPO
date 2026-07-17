namespace CMS.API.Models;

/// <summary>
/// Search DTO for the board (<c>POST /api/featured-promo-items/query</c>). The board loads one
/// TrainingCenter tab and one Monday–Sunday week at a time.
/// </summary>
public class FeaturedPromoItemQuery
{
    /// <summary>訓練中心 tab filter.</summary>
    public short? TrainingCenterPkid { get; set; }

    /// <summary>Week start (Monday) — inclusive lower bound on ScheduleOn.</summary>
    public DateOnly? ScheduleOnFrom { get; set; }

    /// <summary>Week end (Sunday) — inclusive upper bound on ScheduleOn.</summary>
    public DateOnly? ScheduleOnTo { get; set; }

    /// <summary>版位 filter (optional).</summary>
    public byte? Slot { get; set; }
}
