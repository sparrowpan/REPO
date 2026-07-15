using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>Write DTO for creating / updating a <see cref="CourseGroup"/>.</summary>
public class CourseGroupRequest
{
    /// <summary>主代碼 — surrogate key (present on update, ignored on insert).</summary>
    public short Pkid { get; set; }

    /// <summary>群組名稱.</summary>
    [Required]
    [MaxLength(100)]
    public string Description { get; set; } = string.Empty;
}
