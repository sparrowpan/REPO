using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// An <see cref="IPublishStatusRepository"/> whose every member throws, standing in for a Dapper
/// repository that fails against a real database. Used to drive the global exception middleware.
/// </summary>
/// <remarks>
/// <see cref="FailureMessage"/> deliberately reads like a raw SQL Server error — table names, SQL
/// text, and a connection string with a password — so tests can assert that none of it survives
/// into the HTTP response.
/// </remarks>
public sealed class ThrowingPublishStatusRepository : IPublishStatusRepository
{
    /// <summary>The exception text that must never reach a client.</summary>
    public const string FailureMessage =
        "Invalid column name 'Descriptionn'. " +
        "SELECT Pkid, RTRIM(Description) AS Description FROM admin.PublishStatus ORDER BY Pkid; " +
        "Server=.\\SQLEXPRESS;Database=CMS;User Id=sa;Password=sup3rs3cret;";

    /// <summary>A fragment of the SQL text above, for substring assertions.</summary>
    public const string SqlFragment = "FROM admin.PublishStatus";

    /// <summary>A fragment of the connection string above, for substring assertions.</summary>
    public const string ConnectionFragment = "Password=sup3rs3cret";

    private static Exception Boom() => new InvalidOperationException(FailureMessage);

    public Task<IReadOnlyList<PublishStatus>> GetAllAsync(CancellationToken ct = default)
        => throw Boom();

    public Task<IReadOnlyList<PublishStatus>> QueryAsync(PublishStatusQuery query, CancellationToken ct = default)
        => throw Boom();

    public Task<PublishStatus?> GetByPkidAsync(byte pkid, CancellationToken ct = default)
        => throw Boom();

    public Task<bool> PkidExistsAsync(byte pkid, CancellationToken ct = default)
        => throw Boom();

    public Task<PublishStatus> CreateAsync(PublishStatusRequest request, CancellationToken ct = default)
        => throw Boom();

    public Task<bool> UpdateAsync(PublishStatusRequest request, CancellationToken ct = default)
        => throw Boom();

    public Task<bool> DeleteAsync(byte pkid, CancellationToken ct = default)
        => throw Boom();
}
