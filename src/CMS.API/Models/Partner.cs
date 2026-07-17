namespace CMS.API.Models;

/// <summary>Response model for a course partner (合作廠商).</summary>
public class Partner
{
    /// <summary>主代碼 — surrogate identity key.</summary>
    public short Pkid { get; set; }

    /// <summary>廠商名稱.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>廠商代碼.</summary>
    public string AppKey { get; set; } = string.Empty;

    /// <summary>選單顯示名稱.</summary>
    public string NameOnPartnerMenu { get; set; } = string.Empty;

    /// <summary>課程頁顯示名稱.</summary>
    public string NameOnCourseDetailPage { get; set; } = string.Empty;

    /// <summary>顯示順序.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>圖檔名稱.</summary>
    public string? ImageFilename { get; set; }
}
