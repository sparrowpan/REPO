using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end API tests for the AppUser endpoints (list/filter, view, add, edit, delete,
/// reset-password) running against an in-memory fake repository. Each test gets an isolated
/// factory so mutations don't leak between tests.
/// </summary>
public class AppUsersControllerTests
{
    private static (CmsApiFactory factory, HttpClient client) CreateClient()
    {
        var factory = new CmsApiFactory();
        return (factory, factory.CreateClient());
    }

    // --- List / filter ------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsSeededUsers()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var users = await client.GetFromJsonAsync<List<AppUser>>("/api/appusers");

        Assert.NotNull(users);
        Assert.Equal(2, users!.Count);
        Assert.Contains(users, u => u.UserId == "helen");
        Assert.Contains(users, u => u.UserId == "miles");
    }

    [Fact]
    public async Task GetAll_IncludesRoleCount()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var users = await client.GetFromJsonAsync<List<AppUser>>("/api/appusers");
        var helen = users!.Single(u => u.UserId == "helen");

        Assert.Equal(2, helen.RoleCount);
    }

    [Fact]
    public async Task Query_ByKeyword_FiltersByUserName()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/appusers/query", new AppUserQuery { Keyword = "Miles" });
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<List<AppUser>>();

        Assert.Single(users!);
        Assert.Equal("miles", users!.Single().UserId);
    }

    [Fact]
    public async Task Query_ByIsActive_FiltersExact()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/appusers/query", new AppUserQuery { IsActive = false });
        var users = await response.Content.ReadFromJsonAsync<List<AppUser>>();

        Assert.Single(users!);
        Assert.Equal("miles", users!.Single().UserId);
    }

    [Fact]
    public async Task Query_EmptyFilter_ReturnsAll()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/appusers/query", new AppUserQuery());
        var users = await response.Content.ReadFromJsonAsync<List<AppUser>>();

        Assert.Equal(2, users!.Count);
    }

    // --- View ---------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingUser_ReturnsUserWithRoleIds()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var user = await client.GetFromJsonAsync<AppUser>("/api/appusers/helen");

        Assert.NotNull(user);
        Assert.Equal("helen", user!.UserId);
        Assert.Equal("Helen Wang", user.UserName);
        Assert.True(user.IsActive);
        Assert.Equal(2, user.RoleIds.Count);
        Assert.Contains("Admin", user.RoleIds);
    }

    [Fact]
    public async Task GetById_UnknownUser_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.GetAsync("/api/appusers/DoesNotExist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Add ----------------------------------------------------------------

    [Fact]
    public async Task Create_NewUser_Returns201AndIsRetrievable()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new AppUserRequest
        {
            UserId = "jenny",
            UserName = "Jenny Tsao",
            IsActive = true,
            RoleIds = ["Admin", "User"],
        };

        var response = await client.PostAsJsonAsync("/api/appusers", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<AppUser>();
        Assert.Equal("jenny", created!.UserId);
        Assert.Equal(2, created.RoleCount);

        var fetched = await client.GetFromJsonAsync<AppUser>("/api/appusers/jenny");
        Assert.Equal("Jenny Tsao", fetched!.UserName);
        Assert.Equal(2, fetched.RoleIds.Count);
    }

    [Fact]
    public async Task Create_DuplicateUserId_Returns409()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new AppUserRequest { UserId = "helen", UserName = "Dup", IsActive = true };

        var response = await client.PostAsJsonAsync("/api/appusers", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns400()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // UserId and UserName are [Required].
        var request = new AppUserRequest { UserId = "", UserName = "", IsActive = true };

        var response = await client.PostAsJsonAsync("/api/appusers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Edit ---------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingUser_Returns204AndPersists()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new AppUserRequest
        {
            Pkid = 2,
            UserId = "miles",
            UserName = "Miles Sun (updated)",
            IsActive = true,
            RoleIds = ["Admin"],
        };

        var response = await client.PutAsJsonAsync("/api/appusers", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetched = await client.GetFromJsonAsync<AppUser>("/api/appusers/miles");
        Assert.Equal("Miles Sun (updated)", fetched!.UserName);
        Assert.True(fetched.IsActive);
        Assert.Single(fetched.RoleIds);
    }

    [Fact]
    public async Task Update_UnknownUser_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new AppUserRequest { UserId = "ghost", UserName = "Ghost", IsActive = true };

        var response = await client.PutAsJsonAsync("/api/appusers", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Delete -------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingUser_Returns204()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/appusers/miles");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var check = await client.GetAsync("/api/appusers/miles");
        Assert.Equal(HttpStatusCode.NotFound, check.StatusCode);
    }

    // --- Reset password -----------------------------------------------------

    [Fact]
    public async Task ResetPassword_ExistingUser_Returns204()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsync("/api/appusers/helen/reset-password", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_UnknownUser_Returns404()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.PostAsync("/api/appusers/ghost/reset-password", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
