using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CMS.API.Models;
using CMS.API.Tests.Fakes;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end tests for the global JWT authorization: every controller requires a valid Bearer
/// token except <c>AuthController</c>, which stays anonymous. Tokens are minted through the real
/// login endpoint (signed with <see cref="FakeAuthRepository.SigningSecret"/>) and validated by the
/// real JwtBearer middleware, so this exercises issuing and validating together.
/// </summary>
public class AuthorizationTests
{
    private static (CmsApiFactory factory, HttpClient client) CreateClient()
    {
        var factory = new CmsApiFactory();
        return (factory, factory.CreateClient());
    }

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new LoginRequest { UserId = "helen", Password = FakeAuthRepository.Password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    // --- Protected endpoints ------------------------------------------------

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.GetAsync("/api/appusers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_Returns200()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var token = await GetTokenAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/appusers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithGarbageToken_Returns401()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/appusers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnotherProtectedController_WithoutToken_Returns401()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // A different controller confirms the requirement is global, not per-controller.
        var response = await client.GetAsync("/api/approles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- AuthController stays anonymous -------------------------------------

    [Fact]
    public async Task Login_WithoutToken_IsReachable()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // No Authorization header — the login endpoint must still authenticate and issue a token.
        var response = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new LoginRequest { UserId = "helen", Password = FakeAuthRepository.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_BadCredentialsWithoutToken_Returns401NotAuthChallenge()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // Anonymous access reaches the controller, so a bad login returns the controller's own
        // "invalid credentials" 401 — not a middleware auth challenge.
        var response = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new LoginRequest { UserId = "helen", Password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid credentials", body, StringComparison.OrdinalIgnoreCase);
    }
}
