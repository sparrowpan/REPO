using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Services;
using CMS.API.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end API tests for the AppUser endpoints (list/filter, view, add, edit, delete,
/// reset-password) running against an in-memory fake repository. Each test gets an isolated
/// factory so mutations don't leak between tests.
/// </summary>
public class AppUsersControllerTests
{
    /// <summary>The AppUser endpoints are Admin-only, so the default client here carries that role.</summary>
    private static (CmsApiFactory factory, HttpClient client) CreateClient()
    {
        var factory = new CmsApiFactory();
        return (factory, factory.CreateAuthenticatedClient("Admin"));
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

    // --- Admin-only enforcement ---------------------------------------------

    /// <summary>
    /// The whole controller is Admin-only. Update writes RoleIds straight to the AppUserRole
    /// junction, so a merely-authenticated caller reaching it could grant themselves any role —
    /// these pin the gate shut. Theory rather than one test per verb so a newly added action that
    /// forgets the gate shows up as a named failure.
    /// </summary>
    [Theory]
    [InlineData("GET", "/api/appusers")]
    [InlineData("POST", "/api/appusers/query")]
    [InlineData("GET", "/api/appusers/helen")]
    [InlineData("POST", "/api/appusers")]
    [InlineData("PUT", "/api/appusers")]
    [InlineData("DELETE", "/api/appusers/helen")]
    [InlineData("POST", "/api/appusers/helen/reset-password")]
    public async Task EveryAction_AsNonAdmin_Returns403(string method, string url)
    {
        var factory = new CmsApiFactory();
        using var _ = factory;
        // Authenticated, but without the Admin role — must be forbidden, not merely unauthorized.
        var client = factory.CreateAuthenticatedClient("User");

        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// The escalation this gate exists to stop: a non-Admin PUTs their own record with
    /// RoleIds ["Admin"]. It must be refused at the door, and the role set must be untouched.
    /// Uses "miles" — seeded with ["User"] only, so the post-condition is not vacuous the way
    /// it would be for "helen", who is seeded as an Admin already.
    /// </summary>
    [Fact]
    public async Task Update_AsNonAdmin_CannotGrantSelfAdminRole()
    {
        var factory = new CmsApiFactory();
        using var _ = factory;
        var client = factory.CreateAuthenticatedClientAs("miles", "Miles Sun", "User");

        var response = await client.PutAsJsonAsync("/api/appusers", new AppUserRequest
        {
            UserId = "miles",
            UserName = "Miles Sun",
            IsActive = true,
            RoleIds = ["Admin"],
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // The junction must be untouched — the request never reached the repository.
        var fake = (FakeAppUserRepository)factory.Services.GetRequiredService<IAppUserRepository>();
        var miles = await fake.GetByUserIdAsync("miles");
        Assert.DoesNotContain("Admin", miles!.RoleIds);
    }

    // --- Reset password (Admin-only) ----------------------------------------

    private static (CmsApiFactory factory, HttpClient client) AdminClient()
    {
        var factory = new CmsApiFactory();
        return (factory, factory.CreateAuthenticatedClient("Admin"));
    }

    [Fact]
    public async Task ResetPassword_AsAdmin_Returns204()
    {
        var (factory, client) = AdminClient();
        using var _ = factory;

        var response = await client.PostAsync("/api/appusers/helen/reset-password", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_AsNonAdmin_Returns403()
    {
        var factory = new CmsApiFactory();
        using var _ = factory;
        // Authenticated, but without the Admin role — must be forbidden, not merely unauthorized.
        var client = factory.CreateAuthenticatedClient("User");

        var response = await client.PostAsync("/api/appusers/helen/reset-password", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithoutToken_Returns401()
    {
        var factory = new CmsApiFactory();
        using var _ = factory;
        var client = factory.CreateClient(); // no Authorization header

        var response = await client.PostAsync("/api/appusers/helen/reset-password", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_AsAdmin_SetsHashToSha256OfDefault_AndStampsUpdateTime()
    {
        var (factory, client) = AdminClient();
        using var _ = factory;
        var fake = (FakeAppUserRepository)factory.Services.GetRequiredService<IAppUserRepository>();

        var before = (await client.GetFromJsonAsync<AppUser>("/api/appusers/helen"))!.PasswordUpdatedTime;

        var response = await client.PostAsync("/api/appusers/helen/reset-password", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // PasswordHash is exactly SHA-256 of the SysConfig default password...
        Assert.Equal(PasswordHasher.Hash(FakeAppUserRepository.DefaultPassword), fake.GetPasswordHash("helen"));

        // ...and PasswordUpdatedTime advanced past the seed value.
        var after = (await client.GetFromJsonAsync<AppUser>("/api/appusers/helen"))!.PasswordUpdatedTime;
        Assert.True(after > before, "PasswordUpdatedTime should move forward after a reset.");
    }

    [Fact]
    public async Task ResetPassword_AsAdmin_ResponseNeverContainsPasswordOrHash()
    {
        var (factory, client) = AdminClient();
        using var _ = factory;

        var response = await client.PostAsync("/api/appusers/helen/reset-password", null);
        var raw = await response.Content.ReadAsStringAsync();

        // 204 carries no body — and certainly no plaintext default or hash.
        Assert.True(string.IsNullOrEmpty(raw));
        Assert.DoesNotContain(FakeAppUserRepository.DefaultPassword, raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            PasswordHasher.Hash(FakeAppUserRepository.DefaultPassword), raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResetPassword_UnknownUser_AsAdmin_Returns404()
    {
        var (factory, client) = AdminClient();
        using var _ = factory;

        var response = await client.PostAsync("/api/appusers/ghost/reset-password", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
