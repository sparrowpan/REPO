using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IPartnerRepository"/> so the API can be exercised end-to-end
/// (routing, model binding, controller behavior) without a SQL Server instance.
/// </summary>
public sealed class FakePartnerRepository : IPartnerRepository
{
    private readonly List<Partner> _partners = [];
    private short _nextPkid = 1;

    public FakePartnerRepository()
    {
        Seed("恆逸", "UUU", "恆逸教育訓練中心", "恆逸", 1, "uuu.png");
        Seed("巨匠", "PCS", "巨匠電腦", "巨匠", 2, null);
    }

    private void Seed(string name, string appKey, string menuName, string courseName, int order, string? image)
        => _partners.Add(new Partner
        {
            Pkid = _nextPkid++,
            Name = name,
            AppKey = appKey,
            NameOnPartnerMenu = menuName,
            NameOnCourseDetailPage = courseName,
            DisplayOrder = order,
            ImageFilename = image,
        });

    public Task<IReadOnlyList<Partner>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Partner>>(_partners.OrderBy(p => p.DisplayOrder).ToList());

    public Task<IReadOnlyList<Partner>> QueryAsync(PartnerQuery query, CancellationToken ct = default)
    {
        IEnumerable<Partner> result = _partners;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(p =>
                p.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                p.AppKey.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                p.NameOnPartnerMenu.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                p.NameOnCourseDetailPage.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<Partner>>(result.OrderBy(p => p.DisplayOrder).ToList());
    }

    public Task<Partner?> GetByPkidAsync(short pkid, CancellationToken ct = default)
        => Task.FromResult(Find(pkid));

    public Task<Partner> CreateAsync(PartnerRequest request, CancellationToken ct = default)
    {
        var partner = new Partner
        {
            Pkid = _nextPkid++,
            Name = request.Name,
            AppKey = request.AppKey,
            NameOnPartnerMenu = request.NameOnPartnerMenu,
            NameOnCourseDetailPage = request.NameOnCourseDetailPage,
            DisplayOrder = request.DisplayOrder,
            ImageFilename = request.ImageFilename,
        };
        _partners.Add(partner);
        return Task.FromResult(partner);
    }

    public Task<bool> UpdateAsync(PartnerRequest request, CancellationToken ct = default)
    {
        var partner = Find(request.Pkid);
        if (partner is null)
            return Task.FromResult(false);

        partner.Name = request.Name;
        partner.AppKey = request.AppKey;
        partner.NameOnPartnerMenu = request.NameOnPartnerMenu;
        partner.NameOnCourseDetailPage = request.NameOnCourseDetailPage;
        partner.DisplayOrder = request.DisplayOrder;
        partner.ImageFilename = request.ImageFilename;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(short pkid, CancellationToken ct = default)
    {
        var partner = Find(pkid);
        if (partner is null)
            return Task.FromResult(false);

        _partners.Remove(partner);
        return Task.FromResult(true);
    }

    private Partner? Find(short pkid) => _partners.FirstOrDefault(p => p.Pkid == pkid);
}
