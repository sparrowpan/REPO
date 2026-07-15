using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>Write DTO for creating / updating a <see cref="Partner"/>.</summary>
public class PartnerRequest
{
    /// <summary>主代碼 — surrogate key (present on update, ignored on insert).</summary>
    public short Pkid { get; set; }

    /// <summary>廠商名稱.</summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>廠商代碼.</summary>
    [Required]
    [MaxLength(10)]
    public string AppKey { get; set; } = string.Empty;

    /// <summary>選單顯示名稱.</summary>
    [Required]
    [MaxLength(200)]
    public string NameOnPartnerMenu { get; set; } = string.Empty;

    /// <summary>課程頁顯示名稱.</summary>
    [Required]
    [MaxLength(50)]
    public string NameOnCourseDetailPage { get; set; } = string.Empty;

    /// <summary>顯示順序.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>圖檔名稱.</summary>
    [MaxLength(50)]
    public string? ImageFilename { get; set; }
}
