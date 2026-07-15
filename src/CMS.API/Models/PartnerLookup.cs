namespace CMS.API.Models;

/// <summary>Slim lookup item for Partner, used as the FK target of Course / Certification.</summary>
public class PartnerLookup
{
    /// <summary>主代碼 — value stored in the referencing FK column.</summary>
    public short Pkid { get; set; }

    /// <summary>廠商名稱 — display label.</summary>
    public string Name { get; set; } = string.Empty;
}
