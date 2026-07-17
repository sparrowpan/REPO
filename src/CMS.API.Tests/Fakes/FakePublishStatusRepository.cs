using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IPublishStatusRepository"/> so the API can be exercised end-to-end
/// (routing, model binding, controller behavior) without a SQL Server instance.
/// </summary>
public sealed class FakePublishStatusRepository : IPublishStatusRepository
{
    private readonly List<PublishStatus> _statuses = [];

    public FakePublishStatusRepository()
    {
        Seed(1, "草稿", isDraft: true, isPublished: false, isDiscontinued: false);
        Seed(2, "已發布", isDraft: false, isPublished: true, isDiscontinued: false);
        Seed(3, "已停用", isDraft: false, isPublished: false, isDiscontinued: true);
    }

    private void Seed(byte pkid, string description, bool isDraft, bool isPublished, bool isDiscontinued)
        => _statuses.Add(new PublishStatus
        {
            Pkid = pkid,
            Description = description,
            IsDraft = isDraft,
            IsPublished = isPublished,
            IsDiscontinued = isDiscontinued,
        });

    public Task<IReadOnlyList<PublishStatus>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PublishStatus>>(_statuses.OrderBy(s => s.Pkid).ToList());

    public Task<IReadOnlyList<PublishStatus>> QueryAsync(PublishStatusQuery query, CancellationToken ct = default)
    {
        IEnumerable<PublishStatus> result = _statuses;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(s => s.Description.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.IsDraft is { } draft)
            result = result.Where(s => s.IsDraft == draft);
        if (query.IsPublished is { } published)
            result = result.Where(s => s.IsPublished == published);
        if (query.IsDiscontinued is { } discontinued)
            result = result.Where(s => s.IsDiscontinued == discontinued);

        return Task.FromResult<IReadOnlyList<PublishStatus>>(result.OrderBy(s => s.Pkid).ToList());
    }

    public Task<PublishStatus?> GetByPkidAsync(byte pkid, CancellationToken ct = default)
        => Task.FromResult(Find(pkid));

    public Task<bool> PkidExistsAsync(byte pkid, CancellationToken ct = default)
        => Task.FromResult(Find(pkid) is not null);

    public Task<PublishStatus> CreateAsync(PublishStatusRequest request, CancellationToken ct = default)
    {
        var status = new PublishStatus
        {
            Pkid = request.Pkid,
            Description = request.Description,
            IsDraft = request.IsDraft,
            IsPublished = request.IsPublished,
            IsDiscontinued = request.IsDiscontinued,
        };
        _statuses.Add(status);
        return Task.FromResult(status);
    }

    public Task<bool> UpdateAsync(PublishStatusRequest request, CancellationToken ct = default)
    {
        var status = Find(request.Pkid);
        if (status is null)
            return Task.FromResult(false);

        status.Description = request.Description;
        status.IsDraft = request.IsDraft;
        status.IsPublished = request.IsPublished;
        status.IsDiscontinued = request.IsDiscontinued;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(byte pkid, CancellationToken ct = default)
    {
        var status = Find(pkid);
        if (status is null)
            return Task.FromResult(false);

        _statuses.Remove(status);
        return Task.FromResult(true);
    }

    private PublishStatus? Find(byte pkid) => _statuses.FirstOrDefault(s => s.Pkid == pkid);
}
