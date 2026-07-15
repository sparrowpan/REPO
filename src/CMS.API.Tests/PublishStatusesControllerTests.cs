using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end API tests for the PublishStatus endpoints (list/filter, view, add, edit, delete)
/// running against an in-memory fake repository. Each test gets an isolated factory
/// so mutations don't leak between tests.
/// </summary>
public class PublishStatusesControllerTests
{
    private static (CmsApiFactory factory, HttpClient client) CreateClient()
    {
        var factory = new CmsApiFactory();
        return (factory, factory.CreateClient());
    }

    // --- List / filter ------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsSeededStatuses()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var statuses = await client.GetFromJsonAsync<List<PublishStatus>>("/api/publish-statuses");

        Assert.NotNull(statuses);
        Assert.Equal(3, statuses!.Count);
        Assert.Contains(statuses, s => s.Pkid == 1 && s.Description == "草稿");
    }

    [Fact]
    public async Task Query_ByKeyword_FiltersByDescription()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/publish-statuses/query", new PublishStatusQuery { Keyword = "發布" });
        response.EnsureSuccessStatusCode();
        var statuses = await response.Content.ReadFromJsonAsync<List<PublishStatus>>();

        Assert.Single(statuses!);
        Assert.Equal(2, statuses!.Single().Pkid);
    }

    [Fact]
    public async Task Query_ByIsPublished_FiltersExact()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/publish-statuses/query", new PublishStatusQuery { IsPublished = true });
        var statuses = await response.Content.ReadFromJsonAsync<List<PublishStatus>>();

        Assert.Single(statuses!);
        Assert.Equal("已發布", statuses!.Single().Description);
    }

    [Fact]
    public async Task Query_EmptyFilter_ReturnsAll()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/publish-statuses/query", new PublishStatusQuery());
        var statuses = await response.Content.ReadFromJsonAsync<List<PublishStatus>>();

        Assert.Equal(3, statuses!.Count);
    }

    // --- View ---------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingStatus_ReturnsStatus()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var status = await client.GetFromJsonAsync<PublishStatus>("/api/publish-statuses/1");

        Assert.NotNull(status);
        Assert.Equal(1, status!.Pkid);
        Assert.Equal("草稿", status.Description);
        Assert.True(status.IsDraft);
    }

    [Fact]
    public async Task GetById_UnknownStatus_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.GetAsync("/api/publish-statuses/99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Add ----------------------------------------------------------------

    [Fact]
    public async Task Create_NewStatus_Returns201AndIsRetrievable()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new PublishStatusRequest
        {
            Pkid = 4,
            Description = "已封存",
            IsDraft = false,
            IsPublished = false,
            IsDiscontinued = true,
        };

        var response = await client.PostAsJsonAsync("/api/publish-statuses", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<PublishStatus>();
        Assert.Equal(4, created!.Pkid);

        var fetched = await client.GetFromJsonAsync<PublishStatus>("/api/publish-statuses/4");
        Assert.Equal("已封存", fetched!.Description);
        Assert.True(fetched.IsDiscontinued);
    }

    [Fact]
    public async Task Create_DuplicatePkid_Returns409()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new PublishStatusRequest { Pkid = 1, Description = "Dup" };

        var response = await client.PostAsJsonAsync("/api/publish-statuses", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingDescription_Returns400()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // Description is [Required].
        var request = new PublishStatusRequest { Pkid = 5, Description = "" };

        var response = await client.PostAsJsonAsync("/api/publish-statuses", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Edit ---------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingStatus_Returns204AndPersists()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new PublishStatusRequest
        {
            Pkid = 2,
            Description = "已上線",
            IsDraft = false,
            IsPublished = true,
            IsDiscontinued = false,
        };

        var response = await client.PutAsJsonAsync("/api/publish-statuses", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetched = await client.GetFromJsonAsync<PublishStatus>("/api/publish-statuses/2");
        Assert.Equal("已上線", fetched!.Description);
    }

    [Fact]
    public async Task Update_UnknownStatus_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new PublishStatusRequest { Pkid = 88, Description = "Ghost" };

        var response = await client.PutAsJsonAsync("/api/publish-statuses", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Delete -------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingStatus_Returns204()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/publish-statuses/3");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var check = await client.GetAsync("/api/publish-statuses/3");
        Assert.Equal(HttpStatusCode.NotFound, check.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownStatus_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/publish-statuses/77");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
