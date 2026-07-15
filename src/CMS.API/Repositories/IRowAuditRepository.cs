using CMS.API.Models;

namespace CMS.API.Repositories;

/// <summary>Read access to a single record's <see cref="RowAudit"/> history.</summary>
public interface IRowAuditRepository
{
    /// <summary>
    /// The audit trail for one record (matched on <paramref name="tableName"/> + the record's surrogate
    /// <paramref name="pkid"/>), newest first. Empty when the record has no history.
    /// </summary>
    Task<IReadOnlyList<RowAuditEntry>> GetForRecordAsync(string tableName, int pkid, CancellationToken ct = default);
}
