using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end API tests for the AppRole endpoints (list/filter, view, add, edit)
/// running against an in-memory fake repository. Each test gets an isolated factory
/// so mutations don't leak between tests.
/// </summary>
public class AppRolesControllerTests
{
    private static (CmsApiFactory factory, HttpClient client) CreateClient()
    {
        var factory = new CmsApiFactory();
        return (factory, factory.CreateAuthenticatedClient());
    }

    // --- List / filter ------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsSeededRoles()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var roles = await client.GetFromJsonAsync<List<AppRole>>("/api/approles");

        Assert.NotNull(roles);
        Assert.Equal(2, roles!.Count);
        Assert.Contains(roles, r => r.RoleId == "Admin");
        Assert.Contains(roles, r => r.RoleId == "User");
    }

    [Fact]
    public async Task GetAll_IncludesUserCount()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var roles = await client.GetFromJsonAsync<List<AppRole>>("/api/approles");
        var admin = roles!.Single(r => r.RoleId == "Admin");

        Assert.Equal(3, admin.UserCount);
    }

    [Fact]
    public async Task Query_ByKeyword_FiltersByRoleName()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/approles/query", new AppRoleQuery { Keyword = "Administrator" });
        response.EnsureSuccessStatusCode();
        var roles = await response.Content.ReadFromJsonAsync<List<AppRole>>();

        Assert.Single(roles!);
        Assert.Equal("Admin", roles!.Single().RoleId);
    }

    [Fact]
    public async Task Query_ByPermissionLevel_FiltersExact()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/approles/query", new AppRoleQuery { PermissionLevel = 100 });
        var roles = await response.Content.ReadFromJsonAsync<List<AppRole>>();

        Assert.Single(roles!);
        Assert.Equal("User", roles!.Single().RoleId);
    }

    [Fact]
    public async Task Query_EmptyFilter_ReturnsAll()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/approles/query", new AppRoleQuery());
        var roles = await response.Content.ReadFromJsonAsync<List<AppRole>>();

        Assert.Equal(2, roles!.Count);
    }

    // --- View ---------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingRole_ReturnsRoleWithUserIds()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var role = await client.GetFromJsonAsync<AppRole>("/api/approles/Admin");

        Assert.NotNull(role);
        Assert.Equal("Admin", role!.RoleId);
        Assert.Equal("Administrator", role.RoleName);
        Assert.Equal(1, role.PermissionLevel);
        Assert.Equal(3, role.UserIds.Count);
        Assert.Contains("helen", role.UserIds);
    }

    [Fact]
    public async Task GetById_UnknownRole_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.GetAsync("/api/approles/DoesNotExist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Add ----------------------------------------------------------------

    [Fact]
    public async Task Create_NewRole_Returns201AndIsRetrievable()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new AppRoleRequest
        {
            RoleId = "Editor",
            RoleName = "Content Editor",
            PermissionLevel = 50,
            Description = "編輯者",
            UserIds = ["helen", "miles"],
        };

        var response = await client.PostAsJsonAsync("/api/approles", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<AppRole>();
        Assert.Equal("Editor", created!.RoleId);
        Assert.Equal(2, created.UserCount);

        var fetched = await client.GetFromJsonAsync<AppRole>("/api/approles/Editor");
        Assert.Equal("Content Editor", fetched!.RoleName);
        Assert.Equal(2, fetched.UserIds.Count);
    }

    [Fact]
    public async Task Create_DuplicateRoleId_Returns409()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new AppRoleRequest { RoleId = "Admin", RoleName = "Dup", PermissionLevel = 1 };

        var response = await client.PostAsJsonAsync("/api/approles", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns400()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // RoleId and RoleName are [Required].
        var request = new AppRoleRequest { RoleId = "", RoleName = "", PermissionLevel = 10 };

        var response = await client.PostAsJsonAsync("/api/approles", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Edit ---------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingRole_Returns204AndPersists()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new AppRoleRequest
        {
            Pkid = 2,
            RoleId = "User",
            RoleName = "General User",
            PermissionLevel = 200,
            Description = "更新後描述",
            UserIds = ["helen", "Jenny_Tsao"],
        };

        var response = await client.PutAsJsonAsync("/api/approles", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetched = await client.GetFromJsonAsync<AppRole>("/api/approles/User");
        Assert.Equal("General User", fetched!.RoleName);
        Assert.Equal(200, fetched.PermissionLevel);
        Assert.Equal(2, fetched.UserIds.Count);
    }

    [Fact]
    public async Task Update_UnknownRole_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new AppRoleRequest { RoleId = "Ghost", RoleName = "Ghost", PermissionLevel = 1 };

        var response = await client.PutAsJsonAsync("/api/approles", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Delete -------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingRole_Returns204()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/approles/User");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var check = await client.GetAsync("/api/approles/User");
        Assert.Equal(HttpStatusCode.NotFound, check.StatusCode);
    }
}
