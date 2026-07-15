namespace CMS.API.Models;

/// <summary>Slim lookup item for JobCategory, used as the n-n target of Course.</summary>
public class JobCategoryLookup
{
    /// <summary>主代碼 — value stored in the CourseJobCategories junction row.</summary>
    public short Pkid { get; set; }

    /// <summary>職務類別 — display label.</summary>
    public string Description { get; set; } = string.Empty;
}
