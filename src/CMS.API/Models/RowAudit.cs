namespace CMS.API.Models;

/// <summary>
/// One audit row describing a single Insert / Update / Delete against a business table.
/// Written by <see cref="Services.RowAuditWriter"/>. <see cref="Pkid"/> is IDENTITY and is
/// never inserted; <see cref="ActionTime"/> maps to the SQL <c>[DateTime]</c> column.
/// </summary>
public sealed class RowAudit
{
    /// <summary>IDENTITY key — populated by SQL Server, never inserted.</summary>
    public int Pkid { get; set; }

    /// <summary>Name of the business table that changed (e.g. "Course").</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Signed-in user's name from the JWT, or "system" when unauthenticated.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>The changed entity's pkid, as a string.</summary>
    public string PrimaryKeyValues { get; set; } = string.Empty;

    /// <summary>"Insert" | "Update" | "Delete".</summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Insert/Delete: the first string property's value. Update: comma-separated changed
    /// property names. Capped at <see cref="RowAuditWriter.ActionDescMaxLength"/> characters.
    /// </summary>
    public string? ActionDesc { get; set; }

    /// <summary>When the change happened (maps to the <c>[DateTime]</c> column).</summary>
    public DateTime ActionTime { get; set; }
}
