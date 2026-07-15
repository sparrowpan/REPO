using System.Net;
using System.Net.Http.Json;
using System.Text;
using CMS.API.Models;
using CMS.API.Tests.Fakes;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end tests for <c>PUT /api/Auth/profile</c> against the in-memory fake repository.
/// Verifies the endpoint renames the JWT user (never a UserId from the body), rejects an
/// empty/whitespace name, trims, and is protected (401 without a token).
/// </summary>
public class AuthProfileControllerTests
{
    private const string ProfileUrl = "/api/Auth/profile";

    private static (CmsApiFactory factory, HttpClient client) AuthenticatedAsHelen()
    {
        var factory = new CmsApiFactory();
        // Token identity matches the seeded active user "helen" so the update can find a row.
        var client = factory.CreateAuthenticatedClientAs("helen", "Helen Wang", "Admin", "User");
        return (factory, client);
    }

    [Fact]
    public async Task UpdateProfile_ChangesUserName_ForJwtUser()
    {
        var (factory, client) = AuthenticatedAsHelen();
        using var _ = factory;

        var response = await client.PutAsJsonAsync(
            ProfileUrl, new UpdateProfileRequest { UserName = "Helen W. Chang" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(body);
        Assert.Equal("helen", body!.UserId);
        Assert.Equal("Helen W. Chang", body.UserName);

        // Persisted: a fresh login for helen now returns the new name.
        var login = await client.PostAsJsonAsync(
            "/api/Auth/login", new LoginRequest { UserId = "helen", Password = FakeAuthRepository.Password });
        var profile = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("Helen W. Chang", profile!.UserName);
    }

    [Fact]
    public async Task UpdateProfile_IgnoresUserIdInBody_AndUpdatesJwtUser()
    {
        var (factory, client) = AuthenticatedAsHelen();
        using var _ = factory;

        // Body smuggles a different UserId; the endpoint must ignore it and act on the JWT user (helen).
        var content = new StringContent(
            "{\"userId\":\"miles\",\"userName\":\"Hijacked Name\"}", Encoding.UTF8, "application/json");
        var response = await client.PutAsync(ProfileUrl, content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("helen", body!.UserId);
        Assert.NotEqual("miles", body.UserId);

        // helen (the JWT user) was renamed...
        var helenLogin = await client.PostAsJsonAsync(
            "/api/Auth/login", new LoginRequest { UserId = "helen", Password = FakeAuthRepository.Password });
        var helen = await helenLogin.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("Hijacked Name", helen!.UserName);
    }

    [Fact]
    public async Task UpdateProfile_EmptyUserName_Returns400()
    {
        var (factory, client) = AuthenticatedAsHelen();
        using var _ = factory;

        var response = await client.PutAsJsonAsync(
            ProfileUrl, new UpdateProfileRequest { UserName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WhitespaceUserName_Returns400()
    {
        var (factory, client) = AuthenticatedAsHelen();
        using var _ = factory;

        var response = await client.PutAsJsonAsync(
            ProfileUrl, new UpdateProfileRequest { UserName = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_TrimsUserName_BeforeSaving()
    {
        var (factory, client) = AuthenticatedAsHelen();
        using var _ = factory;

        var response = await client.PutAsJsonAsync(
            ProfileUrl, new UpdateProfileRequest { UserName = "  Helen Wang  " });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal("Helen Wang", body!.UserName);
    }

    [Fact]
    public async Task UpdateProfile_WithoutToken_Returns401()
    {
        var factory = new CmsApiFactory();
        using var _ = factory;
        var client = factory.CreateClient(); // no Authorization header

        var response = await client.PutAsJsonAsync(
            ProfileUrl, new UpdateProfileRequest { UserName = "Anyone" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
