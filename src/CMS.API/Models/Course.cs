namespace CMS.API.Models;

/// <summary>Response model for a course (課程), including FK nav objects and n-n counts / lists.</summary>
public class Course
{
    public int Pkid { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OfficialTitle { get; set; }
    public string CourseId { get; set; } = string.Empty;
    public string ProdCourseId { get; set; } = string.Empty;
    public string FriendlyUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    // Foreign keys (raw values; nav objects below carry the display labels).
    public short PartnerPkid { get; set; }
    public short? CourseGroupPkid { get; set; }
    public byte PublishStatusPkid { get; set; }

    public DateOnly ScheduleOn { get; set; }
    public DateOnly ScheduleOff { get; set; }
    public short Hour { get; set; }
    public decimal ListPrice { get; set; }
    public decimal LearningCredit { get; set; }

    public string? Material { get; set; }
    public string? Objective { get; set; }
    public string? Target { get; set; }
    public string? Prerequisites { get; set; }
    public string? Outline { get; set; }
    public string? TowardCertOrExam { get; set; }
    public string? Note { get; set; }
    public string? OtherInfo { get; set; }
    public bool CanRepeat { get; set; }

    // FK nav objects (Dapper multi-map).
    public PartnerLookup? Partner { get; set; }
    public CourseGroupLookup? CourseGroup { get; set; }
    public PublishStatusLookup? PublishStatus { get; set; }

    // N-N: counts on list/query; pkid lists + label lists on GetById.
    public int JobCategoryCount { get; set; }
    public int CertificationCount { get; set; }
    public List<short> JobCategoryPkids { get; set; } = new();
    public List<int> CertificationPkids { get; set; } = new();
    public List<JobCategoryLookup> JobCategories { get; set; } = new();
    public List<CertificationLookup> Certifications { get; set; } = new();
}
