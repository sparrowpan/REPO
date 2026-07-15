using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end API tests for the Partner endpoints (list/filter, view, add, edit, delete)
/// running against an in-memory fake repository. Each test gets an isolated factory
/// so mutations don't leak between tests.
/// </summary>
public class PartnersControllerTests
{
    private static (CmsApiFactory factory, HttpClient client) CreateClient()
    {
        var factory = new CmsApiFactory();
        return (factory, factory.CreateClient());
    }

    // --- List / filter ------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsSeededPartners()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var partners = await client.GetFromJsonAsync<List<Partner>>("/api/partners");

        Assert.NotNull(partners);
        Assert.Equal(2, partners!.Count);
        Assert.Contains(partners, p => p.Name == "恆逸");
    }

    [Fact]
    public async Task GetAll_OrdersByDisplayOrder()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var partners = await client.GetFromJsonAsync<List<Partner>>("/api/partners");

        Assert.Equal("恆逸", partners![0].Name);
        Assert.Equal("巨匠", partners![1].Name);
    }

    [Fact]
    public async Task Query_ByKeyword_FiltersByAppKey()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/partners/query", new PartnerQuery { Keyword = "PCS" });
        response.EnsureSuccessStatusCode();
        var partners = await response.Content.ReadFromJsonAsync<List<Partner>>();

        Assert.Single(partners!);
        Assert.Equal("巨匠", partners!.Single().Name);
    }

    [Fact]
    public async Task Query_EmptyFilter_ReturnsAll()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/partners/query", new PartnerQuery());
        var partners = await response.Content.ReadFromJsonAsync<List<Partner>>();

        Assert.Equal(2, partners!.Count);
    }

    // --- View ---------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingPartner_ReturnsPartner()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var partner = await client.GetFromJsonAsync<Partner>("/api/partners/1");

        Assert.NotNull(partner);
        Assert.Equal(1, partner!.Pkid);
        Assert.Equal("恆逸", partner.Name);
        Assert.Equal("UUU", partner.AppKey);
    }

    [Fact]
    public async Task GetById_UnknownPartner_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.GetAsync("/api/partners/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Add ----------------------------------------------------------------

    [Fact]
    public async Task Create_NewPartner_Returns201AndIsRetrievable()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new PartnerRequest
        {
            Name = "資策會",
            AppKey = "III",
            NameOnPartnerMenu = "資訊工業策進會",
            NameOnCourseDetailPage = "資策會",
            DisplayOrder = 3,
            ImageFilename = null,
        };

        var response = await client.PostAsJsonAsync("/api/partners", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Partner>();
        Assert.Equal("資策會", created!.Name);
        Assert.True(created.Pkid > 0);

        var fetched = await client.GetFromJsonAsync<Partner>($"/api/partners/{created.Pkid}");
        Assert.Equal("III", fetched!.AppKey);
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns400()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // Name / AppKey / NameOnPartnerMenu / NameOnCourseDetailPage are [Required].
        var request = new PartnerRequest { Name = "", AppKey = "", NameOnPartnerMenu = "", NameOnCourseDetailPage = "", DisplayOrder = 1 };

        var response = await client.PostAsJsonAsync("/api/partners", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Edit ---------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingPartner_Returns204AndPersists()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new PartnerRequest
        {
            Pkid = 2,
            Name = "巨匠電腦",
            AppKey = "PCSCHOOL",
            NameOnPartnerMenu = "巨匠電腦教育",
            NameOnCourseDetailPage = "巨匠",
            DisplayOrder = 5,
            ImageFilename = "pcs.png",
        };

        var response = await client.PutAsJsonAsync("/api/partners", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetched = await client.GetFromJsonAsync<Partner>("/api/partners/2");
        Assert.Equal("巨匠電腦", fetched!.Name);
        Assert.Equal(5, fetched.DisplayOrder);
    }

    [Fact]
    public async Task Update_UnknownPartner_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new PartnerRequest
        {
            Pkid = 88,
            Name = "Ghost",
            AppKey = "GHO",
            NameOnPartnerMenu = "Ghost",
            NameOnCourseDetailPage = "Ghost",
            DisplayOrder = 1,
        };

        var response = await client.PutAsJsonAsync("/api/partners", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Delete -------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingPartner_Returns204()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/partners/2");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var check = await client.GetAsync("/api/partners/2");
        Assert.Equal(HttpStatusCode.NotFound, check.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownPartner_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/partners/77");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
