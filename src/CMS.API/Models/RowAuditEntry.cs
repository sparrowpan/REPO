namespace CMS.API.Models;

/// <summary>
/// A single row of a record's audit history, as returned by <c>GET /api/rowaudit</c>. A lean projection
/// of <see cref="RowAudit"/> — only the columns the UI shows (no <c>TableName</c>/<c>PrimaryKeyValues</c>,
/// which are the query keys). <see cref="DateTime"/> maps from the SQL <c>[DateTime]</c> column.
/// </summary>
public sealed class RowAuditEntry
{
    /// <summary>When the change happened (the SQL <c>[DateTime]</c> column).</summary>
    public DateTime DateTime { get; set; }

    /// <summary>The user who made the change (JWT <c>userName</c>, or "system" when unauthenticated).</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>"Insert" | "Update" | "Delete".</summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>Insert/Delete: the row's first string column. Update: the changed column names.</summary>
    public string? ActionDesc { get; set; }
}
