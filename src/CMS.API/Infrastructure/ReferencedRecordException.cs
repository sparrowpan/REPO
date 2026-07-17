using Microsoft.Data.SqlClient;

namespace CMS.API.Infrastructure;

/// <summary>
/// A delete was refused because other rows still reference the record (SQL Server foreign key
/// violation, error 547). Repositories translate the provider exception into this so controllers
/// can answer <c>409</c> with a message naming the entity, rather than letting a raw
/// <see cref="SqlException"/> escape to the middleware and become a generic <c>500</c>.
/// </summary>
/// <remarks>
/// Repositories throw it, controllers catch it — the same split the existing duplicate-key 409s use.
/// It carries no SQL text: the caller only learns that the row is still in use.
/// </remarks>
public sealed class ReferencedRecordException(string tableName, Exception? innerException = null)
    : Exception($"A {tableName} row is still referenced by other records.", innerException)
{
    /// <summary>SQL Server's error number for a foreign key constraint violation.</summary>
    public const int SqlForeignKeyViolation = 547;

    /// <summary>The table whose row could not be deleted.</summary>
    public string TableName { get; } = tableName;

    /// <summary>True when <paramref name="ex"/> is a SQL Server foreign key violation.</summary>
    public static bool IsForeignKeyViolation(Exception ex) =>
        ex is SqlException sql && sql.Number == SqlForeignKeyViolation;
}
