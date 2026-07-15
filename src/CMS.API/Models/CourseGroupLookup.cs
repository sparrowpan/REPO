namespace CMS.API.Models;

/// <summary>Slim lookup item for CourseGroup, used as the FK target of Course.</summary>
public class CourseGroupLookup
{
    /// <summary>主代碼 — value stored in the referencing FK column.</summary>
    public short Pkid { get; set; }

    /// <summary>群組名稱 — display label.</summary>
    public string Description { get; set; } = string.Empty;
}
