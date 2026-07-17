using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>Write DTO for creating / updating a <see cref="PublishStatus"/>.</summary>
public class PublishStatusRequest
{
    /// <summary>主代碼 — user-entered key; required on create, immutable on update.</summary>
    public byte Pkid { get; set; }

    /// <summary>狀態說明.</summary>
    [Required]
    [MaxLength(50)]
    public string Description { get; set; } = string.Empty;

    /// <summary>草稿.</summary>
    public bool IsDraft { get; set; }

    /// <summary>已發布.</summary>
    public bool IsPublished { get; set; }

    /// <summary>已停用.</summary>
    public bool IsDiscontinued { get; set; }
}
