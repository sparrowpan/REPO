namespace CMS.API.Models;

/// <summary>Response model for a course group (課程群組).</summary>
public class CourseGroup
{
    /// <summary>主代碼 — surrogate identity key.</summary>
    public short Pkid { get; set; }

    /// <summary>群組名稱.</summary>
    public string Description { get; set; } = string.Empty;
}
