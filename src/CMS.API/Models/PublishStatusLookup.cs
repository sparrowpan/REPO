namespace CMS.API.Models;

/// <summary>Slim lookup item for PublishStatus, used as the FK target of Course / Promotion2.</summary>
public class PublishStatusLookup
{
    /// <summary>主代碼 — value stored in the referencing FK column.</summary>
    public byte Pkid { get; set; }

    /// <summary>狀態說明 — display label.</summary>
    public string Description { get; set; } = string.Empty;
}
