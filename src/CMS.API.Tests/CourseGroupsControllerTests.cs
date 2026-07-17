using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end API tests for the CourseGroup endpoints (list/filter, view, add, edit, delete)
/// running against an in-memory fake repository. Each test gets an isolated factory
/// so mutations don't leak between tests.
/// </summary>
public class CourseGroupsControllerTests
{
    private static (CmsApiFactory factory, HttpClient client) CreateClient()
    {
        var factory = new CmsApiFactory();
        return (factory, factory.CreateAuthenticatedClient());
    }

    // --- List / filter ------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsSeededCourseGroups()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var groups = await client.GetFromJsonAsync<List<CourseGroup>>("/api/course-groups");

        Assert.NotNull(groups);
        Assert.Equal(2, groups!.Count);
        Assert.Contains(groups, g => g.Description == "資訊技術");
    }

    [Fact]
    public async Task GetAll_OrdersByPkid()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var groups = await client.GetFromJsonAsync<List<CourseGroup>>("/api/course-groups");

        Assert.Equal("資訊技術", groups![0].Description);
        Assert.Equal("商業管理", groups![1].Description);
    }

    [Fact]
    public async Task Query_ByKeyword_FiltersByDescription()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/course-groups/query", new CourseGroupQuery { Keyword = "商業" });
        response.EnsureSuccessStatusCode();
        var groups = await response.Content.ReadFromJsonAsync<List<CourseGroup>>();

        Assert.Single(groups!);
        Assert.Equal("商業管理", groups!.Single().Description);
    }

    [Fact]
    public async Task Query_EmptyFilter_ReturnsAll()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/course-groups/query", new CourseGroupQuery());
        var groups = await response.Content.ReadFromJsonAsync<List<CourseGroup>>();

        Assert.Equal(2, groups!.Count);
    }

    // --- View ---------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingCourseGroup_ReturnsCourseGroup()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var group = await client.GetFromJsonAsync<CourseGroup>("/api/course-groups/1");

        Assert.NotNull(group);
        Assert.Equal(1, group!.Pkid);
        Assert.Equal("資訊技術", group.Description);
    }

    [Fact]
    public async Task GetById_UnknownCourseGroup_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.GetAsync("/api/course-groups/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Add ----------------------------------------------------------------

    [Fact]
    public async Task Create_NewCourseGroup_Returns201AndIsRetrievable()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new CourseGroupRequest { Description = "設計創意" };

        var response = await client.PostAsJsonAsync("/api/course-groups", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CourseGroup>();
        Assert.Equal("設計創意", created!.Description);
        Assert.True(created.Pkid > 0);

        var fetched = await client.GetFromJsonAsync<CourseGroup>($"/api/course-groups/{created.Pkid}");
        Assert.Equal("設計創意", fetched!.Description);
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns400()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // Description is [Required].
        var request = new CourseGroupRequest { Description = "" };

        var response = await client.PostAsJsonAsync("/api/course-groups", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Edit ---------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingCourseGroup_Returns204AndPersists()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new CourseGroupRequest { Pkid = 2, Description = "商業與管理" };

        var response = await client.PutAsJsonAsync("/api/course-groups", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetched = await client.GetFromJsonAsync<CourseGroup>("/api/course-groups/2");
        Assert.Equal("商業與管理", fetched!.Description);
    }

    [Fact]
    public async Task Update_UnknownCourseGroup_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new CourseGroupRequest { Pkid = 88, Description = "Ghost" };

        var response = await client.PutAsJsonAsync("/api/course-groups", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Delete -------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingCourseGroup_Returns204()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/course-groups/2");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var check = await client.GetAsync("/api/course-groups/2");
        Assert.Equal(HttpStatusCode.NotFound, check.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownCourseGroup_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/course-groups/77");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
