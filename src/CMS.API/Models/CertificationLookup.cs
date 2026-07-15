namespace CMS.API.Models;

/// <summary>Slim lookup item for Certification, used as the n-n target of Course.</summary>
public class CertificationLookup
{
    /// <summary>主代碼 — value stored in the CourseInCertification junction row.</summary>
    public int Pkid { get; set; }

    /// <summary>認證名稱 — display label (Certification.Title is nchar(100); RTRIM in SQL).</summary>
    public string Title { get; set; } = string.Empty;
}
