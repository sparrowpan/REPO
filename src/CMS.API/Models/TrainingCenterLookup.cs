namespace CMS.API.Models;

/// <summary>Slim TrainingCenter lookup item — drives the board's center tabs.</summary>
public class TrainingCenterLookup
{
    /// <summary>主代碼 — value stored in the referencing FK column.</summary>
    public short Pkid { get; set; }

    /// <summary>訓練中心名稱 — tab label.</summary>
    public string Name { get; set; } = string.Empty;
}
