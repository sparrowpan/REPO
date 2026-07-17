using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IRowAuditRepository"/> so the audit-history endpoint can be exercised end-to-end
/// without SQL Server. Seeds a few records across tables/pkids (with deliberately out-of-order insert
/// times) and returns each record's rows newest first — the contract the controller exposes.
/// </summary>
public sealed class FakeRowAuditRepository : IRowAuditRepository
{
    private readonly record struct Row(string TableName, int Pkid, RowAuditEntry Entry);

    private readonly List<Row> _rows = [];

    public FakeRowAuditRepository()
    {
        // Course/123 — three changes, seeded OUT of chronological order to prove the endpoint sorts.
        Seed("Course", 123, new DateTime(2026, 6, 4, 14, 30, 0), "alice", "Update", "Title, Description");
        Seed("Course", 123, new DateTime(2026, 6, 1, 9, 0, 0), "bob", "Insert", "C# 101");
        Seed("Course", 123, new DateTime(2026, 6, 2, 11, 15, 0), "carol", "Update", "IsPublished");

        // Course/456 — a different record in the same table (must be excluded from a 123 query).
        Seed("Course", 456, new DateTime(2026, 6, 3, 8, 0, 0), "dave", "Insert", "Angular 20");

        // Partner/123 — same pkid, different table (must be excluded from a Course query).
        Seed("Partner", 123, new DateTime(2026, 6, 5, 16, 45, 0), "erin", "Delete", "Acme Corp");
    }

    private void Seed(string tableName, int pkid, DateTime when, string userName, string actionType, string actionDesc)
        => _rows.Add(new Row(tableName, pkid, new RowAuditEntry
        {
            DateTime = when,
            UserName = userName,
            ActionType = actionType,
            ActionDesc = actionDesc,
        }));

    public Task<IReadOnlyList<RowAuditEntry>> GetForRecordAsync(string tableName, int pkid, CancellationToken ct = default)
    {
        var entries = _rows
            .Where(r => r.TableName == tableName && r.Pkid == pkid)
            .Select(r => r.Entry)
            .OrderByDescending(e => e.DateTime) // newest first, as the SQL repository does
            .ToList();
        return Task.FromResult<IReadOnlyList<RowAuditEntry>>(entries);
    }
}
