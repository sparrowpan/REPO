using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>Write DTO for creating / updating a course. FK pkids + n-n pkid lists.</summary>
public class CourseRequest
{
    public int Pkid { get; set; } // present on update, ignored on insert (IDENTITY)

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? OfficialTitle { get; set; }

    [Required, MaxLength(50)]
    public string CourseId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string ProdCourseId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FriendlyUrl { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public short PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte PublishStatusPkid { get; set; }

    public DateOnly ScheduleOn { get; set; }
    public DateOnly ScheduleOff { get; set; }
    public short Hour { get; set; }
    public decimal ListPrice { get; set; }
    public decimal LearningCredit { get; set; }

    [MaxLength(500)]
    public string? Material { get; set; }

    [MaxLength(4000)]
    public string? Objective { get; set; }

    [MaxLength(500)]
    public string? Target { get; set; }

    [MaxLength(4000)]
    public string? Prerequisites { get; set; }

    public string? Outline { get; set; } // nvarchar(max)

    public string? TowardCertOrExam { get; set; } // nvarchar(max)

    [MaxLength(4000)]
    public string? Note { get; set; }

    [MaxLength(4000)]
    public string? OtherInfo { get; set; }

    public bool CanRepeat { get; set; }

    public List<short> JobCategoryPkids { get; set; } = new();
    public List<int> CertificationPkids { get; set; } = new();
}
