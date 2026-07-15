using CMS.API.Infrastructure;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

/// <summary>Dapper-based read access to the <c>dbo.RowAudit</c> table (no EF).</summary>
public sealed class RowAuditRepository(IDbConnectionFactory connectionFactory) : IRowAuditRepository
{
    // PrimaryKeyValues stores the record's surrogate pkid as text (see RowAuditWriter). Order newest first;
    // the pkid tiebreaker keeps same-timestamp rows in insertion order (identity ascending → DESC = newest).
    private const string SelectSql = """
        SELECT [DateTime] AS [DateTime], UserName, ActionType, ActionDesc
        FROM RowAudit
        WHERE TableName = @TableName AND PrimaryKeyValues = @PrimaryKeyValues
        ORDER BY [DateTime] DESC, pkid DESC
        """;

    public async Task<IReadOnlyList<RowAuditEntry>> GetForRecordAsync(string tableName, int pkid, CancellationToken ct = default)
    {
        using var conn = await connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<RowAuditEntry>(new CommandDefinition(
            SelectSql,
            new { TableName = tableName, PrimaryKeyValues = pkid.ToString() },
            cancellationToken: ct));
        return rows.ToList();
    }
}
