using CMS.API.Infrastructure;
using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// Wraps <see cref="FakePublishStatusRepository"/> and refuses to delete <see cref="ReferencedPkid"/>,
/// standing in for SQL Server rejecting the DELETE with a foreign key violation (error 547) because
/// Course rows still reference that status. Every other member delegates to the real fake, so the
/// rest of the endpoint behaves normally.
/// </summary>
/// <remarks>
/// The production repository raises <see cref="ReferencedRecordException"/> by catching a
/// <c>SqlException</c>, which has no public constructor and cannot be fabricated here. Throwing the
/// already-translated domain exception is the seam that keeps the controller's 409 mapping testable
/// without a database; the SqlException-to-domain translation itself is only exercisable against
/// real SQL Server.
/// </remarks>
public sealed class ReferencedPublishStatusRepository : IPublishStatusRepository
{
    /// <summary>The pkid this fake refuses to delete. Any other pkid deletes normally.</summary>
    public const byte ReferencedPkid = 1;

    private readonly FakePublishStatusRepository _inner = new();

    public Task<bool> DeleteAsync(byte pkid, CancellationToken ct = default)
        => pkid == ReferencedPkid
            ? throw new ReferencedRecordException("PublishStatus")
            : _inner.DeleteAsync(pkid, ct);

    public Task<IReadOnlyList<PublishStatus>> GetAllAsync(CancellationToken ct = default)
        => _inner.GetAllAsync(ct);

    public Task<IReadOnlyList<PublishStatus>> QueryAsync(PublishStatusQuery query, CancellationToken ct = default)
        => _inner.QueryAsync(query, ct);

    public Task<PublishStatus?> GetByPkidAsync(byte pkid, CancellationToken ct = default)
        => _inner.GetByPkidAsync(pkid, ct);

    public Task<bool> PkidExistsAsync(byte pkid, CancellationToken ct = default)
        => _inner.PkidExistsAsync(pkid, ct);

    public Task<PublishStatus> CreateAsync(PublishStatusRequest request, CancellationToken ct = default)
        => _inner.CreateAsync(request, ct);

    public Task<bool> UpdateAsync(PublishStatusRequest request, CancellationToken ct = default)
        => _inner.UpdateAsync(request, ct);
}
