using System.Data;
using System.Reflection;
using System.Security.Claims;
using CMS.API.Models;
using Dapper;
using Microsoft.AspNetCore.Http;

namespace CMS.API.Services;

/// <summary>
/// Cross-cutting audit writer: records ONE <see cref="RowAudit"/> row per business-table change.
/// Generic over any entity type via reflection — repositories call <see cref="LogInsert"/> /
/// <see cref="LogUpdate"/> / <see cref="LogDelete"/> after their Insert / Update / Delete, passing
/// the SAME open connection (and transaction) as the change so a rolled-back or failed change
/// leaves no audit row.
/// The acting user is read from the current request's JWT (the <c>userName</c> claim set at login),
/// falling back to <c>"system"</c> when there is no authenticated user.
/// </summary>
public sealed class RowAuditWriter(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>Maximum length of <see cref="RowAudit.ActionDesc"/> (the SQL column is varchar(1000)).</summary>
    public const int ActionDescMaxLength = 1000;

    private const string FallbackUserName = "system";

    private const string InsertSql = """
        INSERT INTO RowAudit (TableName, UserName, PrimaryKeyValues, ActionType, ActionDesc, [DateTime])
        VALUES (@TableName, @UserName, @PrimaryKeyValues, @ActionType, @ActionDesc, @ActionTime);
        """;

    /// <summary>Audit an insert. <c>ActionDesc</c> is the entity's first string property value.</summary>
    public Task LogInsert(string tableName, object entity, IDbConnection conn, IDbTransaction? tx = null, CancellationToken ct = default)
        => WriteAsync(BuildInsertEntry(tableName, entity), conn, tx, ct);

    /// <summary>
    /// Audit an update. <c>ActionDesc</c> lists the property names that changed between
    /// <paramref name="before"/> and <paramref name="after"/>. When nothing changed, no row is written.
    /// </summary>
    public Task LogUpdate(string tableName, object before, object after, IDbConnection conn, IDbTransaction? tx = null, CancellationToken ct = default)
        => WriteAsync(BuildUpdateEntry(tableName, before, after), conn, tx, ct);

    /// <summary>Audit a delete. <c>ActionDesc</c> is the deleted entity's first string property value.</summary>
    public Task LogDelete(string tableName, object entity, IDbConnection conn, IDbTransaction? tx = null, CancellationToken ct = default)
        => WriteAsync(BuildDeleteEntry(tableName, entity), conn, tx, ct);

    // --- Row building (pure; no DB) -----------------------------------------

    internal RowAudit BuildInsertEntry(string tableName, object entity) => new()
    {
        TableName = tableName,
        UserName = ResolveUserName(),
        PrimaryKeyValues = PrimaryKeyValue(entity),
        ActionType = "Insert",
        ActionDesc = Truncate(FirstStringPropertyValue(entity)),
        ActionTime = DateTime.Now,
    };

    internal RowAudit BuildDeleteEntry(string tableName, object entity) => new()
    {
        TableName = tableName,
        UserName = ResolveUserName(),
        PrimaryKeyValues = PrimaryKeyValue(entity),
        ActionType = "Delete",
        ActionDesc = Truncate(FirstStringPropertyValue(entity)),
        ActionTime = DateTime.Now,
    };

    /// <summary>Returns <c>null</c> when nothing changed, so no audit row is written.</summary>
    internal RowAudit? BuildUpdateEntry(string tableName, object before, object after)
    {
        var changed = ChangedPropertyNames(before, after);
        if (changed.Length == 0)
        {
            return null;
        }

        return new RowAudit
        {
            TableName = tableName,
            UserName = ResolveUserName(),
            PrimaryKeyValues = PrimaryKeyValue(after),
            ActionType = "Update",
            ActionDesc = Truncate(changed),
            ActionTime = DateTime.Now,
        };
    }

    private async Task WriteAsync(RowAudit? entry, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        if (entry is null)
        {
            return;
        }

        await conn.ExecuteAsync(new CommandDefinition(InsertSql, entry, tx, cancellationToken: ct));
    }

    // --- Reflection helpers -------------------------------------------------

    /// <summary>The current request's user name from the JWT, or "system" when unauthenticated.</summary>
    internal string ResolveUserName()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var name = user?.FindFirst("userName")?.Value
                   ?? user?.FindFirst(ClaimTypes.Name)?.Value
                   ?? user?.Identity?.Name;

        return string.IsNullOrWhiteSpace(name) ? FallbackUserName : name;
    }

    /// <summary>The entity's pkid property (found case-insensitively) as a string, or "" if absent/null.</summary>
    internal static string PrimaryKeyValue(object entity)
    {
        var prop = ReadableProperties(entity.GetType())
            .FirstOrDefault(p => string.Equals(p.Name, "pkid", StringComparison.OrdinalIgnoreCase));

        return prop?.GetValue(entity)?.ToString() ?? string.Empty;
    }

    /// <summary>Value of the first string-typed property in declaration order, or <c>null</c> if none.</summary>
    internal static string? FirstStringPropertyValue(object entity)
    {
        var prop = ReadableProperties(entity.GetType())
            .FirstOrDefault(p => p.PropertyType == typeof(string));

        return prop?.GetValue(entity) as string;
    }

    /// <summary>
    /// Comma-separated names of the scalar properties whose value differs between <paramref name="before"/>
    /// and <paramref name="after"/>, in declaration order. Empty string when nothing changed. Only scalar
    /// (column-like) properties are compared — navigation objects and collections are ignored, since they
    /// are not columns of the row and reference-compare unequal even when logically the same.
    /// </summary>
    internal static string ChangedPropertyNames(object before, object after)
    {
        var changed = ReadableProperties(before.GetType())
            .Where(p => IsScalar(p.PropertyType))
            .Where(p => !Equals(p.GetValue(before), p.GetValue(after)))
            .Select(p => p.Name);

        return string.Join(", ", changed);
    }

    private static string? Truncate(string? value)
        => value is not null && value.Length > ActionDescMaxLength
            ? value[..ActionDescMaxLength]
            : value;

    /// <summary>Public instance, non-indexer properties in declaration (metadata) order.</summary>
    private static IEnumerable<PropertyInfo> ReadableProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.MetadataToken);

    /// <summary>A "column-like" type: primitives, enums, string, and the common value types (+ their nullables).</summary>
    private static bool IsScalar(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive
            || t.IsEnum
            || t == typeof(string)
            || t == typeof(decimal)
            || t == typeof(DateTime)
            || t == typeof(DateTimeOffset)
            || t == typeof(DateOnly)
            || t == typeof(TimeOnly)
            || t == typeof(Guid);
    }
}
