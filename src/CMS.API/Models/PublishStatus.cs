namespace CMS.API.Models;

/// <summary>Response model for a publishing status (發布狀態).</summary>
public class PublishStatus
{
    /// <summary>主代碼 — user-entered primary key (tinyint, not identity).</summary>
    public byte Pkid { get; set; }

    /// <summary>狀態說明.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>草稿.</summary>
    public bool IsDraft { get; set; }

    /// <summary>已發布.</summary>
    public bool IsPublished { get; set; }

    /// <summary>已停用.</summary>
    public bool IsDiscontinued { get; set; }
}
