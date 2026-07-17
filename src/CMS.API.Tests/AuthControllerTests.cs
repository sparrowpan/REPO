using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CMS.API.Models;
using CMS.API.Services;
using CMS.API.Tests.Fakes;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end tests for <c>POST /api/Auth/login</c> against an in-memory fake repository.
/// Covers the success path (profile + signed JWT), every 401 rejection case, the JWT's role
/// claims and ~24h expiry, and that PasswordHash never leaks into the response.
/// </summary>
public class AuthControllerTests
{
    private static (CmsApiFactory factory, HttpClient client) CreateClient()
    {
        var factory = new CmsApiFactory();
        return (factory, factory.CreateClient());
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string userId, string password)
        => client.PostAsJsonAsync("/api/Auth/login", new LoginRequest { UserId = userId, Password = password });

    // --- Success ------------------------------------------------------------

    [Fact]
    public async Task Login_ValidActiveUser_ReturnsProfileWithToken()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await LoginAsync(client, "helen", FakeAuthRepository.Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.Equal("helen", body!.UserId);
        Assert.Equal("Helen Wang", body.UserName);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
    }

    [Fact]
    public async Task Login_ValidUser_IssuesTokenWithUserAndRoleClaims()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await LoginAsync(client, "helen", FakeAuthRepository.Password);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);

        Assert.Equal("helen", token.Claims.Single(c => c.Type == "userId").Value);
        Assert.Equal("Helen Wang", token.Claims.Single(c => c.Type == "userName").Value);

        var roles = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(2, roles.Count);
        Assert.Contains("Admin", roles);
        Assert.Contains("User", roles);
    }

    [Fact]
    public async Task Login_ValidUser_TokenExpiresIn24Hours()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var before = DateTime.UtcNow;
        var response = await LoginAsync(client, "helen", FakeAuthRepository.Password);
        var after = DateTime.UtcNow;

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);

        // ValidTo should land 24h after issue — bounded by the request window on either side.
        Assert.InRange(
            token.ValidTo,
            before.Add(JwtTokenService.TokenLifetime).AddSeconds(-30),
            after.Add(JwtTokenService.TokenLifetime).AddSeconds(30));
    }

    [Fact]
    public async Task Login_Response_NeverContainsPasswordHash()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await LoginAsync(client, "helen", FakeAuthRepository.Password);
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("passwordhash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(PasswordHasher.Hash(FakeAuthRepository.Password), raw, StringComparison.OrdinalIgnoreCase);
    }

    // --- 401 rejections -----------------------------------------------------

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await LoginAsync(client, "helen", "wrong-password");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await LoginAsync(client, "nobody", FakeAuthRepository.Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InactiveUser_Returns401()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // miles has the correct password but IsActive = false.
        var response = await LoginAsync(client, "miles", FakeAuthRepository.Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_FailedAttempt_DoesNotRevealWhichCheckFailed()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var wrongPassword = await (await LoginAsync(client, "helen", "wrong")).Content.ReadAsStringAsync();
        var unknownUser = await (await LoginAsync(client, "nobody", "wrong")).Content.ReadAsStringAsync();
        var inactive = await (await LoginAsync(client, "miles", FakeAuthRepository.Password)).Content.ReadAsStringAsync();

        // All three rejection cases must surface the identical generic message.
        Assert.Equal(wrongPassword, unknownUser);
        Assert.Equal(unknownUser, inactive);
        Assert.Contains("invalid credentials", wrongPassword, StringComparison.OrdinalIgnoreCase);
    }
}
