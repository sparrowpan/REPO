using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Services;
using CMS.API.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end tests for <c>POST /api/Auth/change-password</c> against the in-memory fake repository.
/// Covers: a wrong current password changes nothing; complexity is enforced (too short, too few
/// character classes); new/confirm mismatch is rejected; a valid change re-hashes the password to
/// SHA-256(new) and stamps <c>PasswordUpdatedTime</c>. The acting user always comes from the JWT.
/// </summary>
public class AuthChangePasswordControllerTests
{
    private const string ChangePasswordUrl = "/api/Auth/change-password";
    private const string LoginUrl = "/api/Auth/login";

    private static (CmsApiFactory factory, HttpClient client) AuthenticatedAsHelen()
    {
        var factory = new CmsApiFactory();
        // Token identity matches the seeded active user "helen" so the change can find a row.
        var client = factory.CreateAuthenticatedClientAs("helen", "Helen Wang", "Admin", "User");
        return (factory, client);
    }

    private static FakeAuthRepository Fake(CmsApiFactory factory)
        => (FakeAuthRepository)factory.Services.GetRequiredService<IAuthRepository>();

    private static ChangePasswordRequest Body(string current, string next, string confirm)
        => new() { CurrentPassword = current, NewPassword = next, ConfirmNewPassword = confirm };

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ChangesNothing()
    {
        var (factory, client) = AuthenticatedAsHelen();
        using var _ = factory;
        var fake = Fake(factory);

        var originalHash = fake.GetPasswordHash("helen");
        var originalTime = fake.GetPasswordUpdatedTime("helen");

        var response = await client.PostAsJsonAsync(
            ChangePasswordUrl, Body("wrong-current", "NewPass123", "NewPass123"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Nothing changed: stored hash and update time are untouched...
        Assert.Equal(originalHash, fake.GetPasswordHash("helen"));
        Assert.Equal(originalTime, fake.GetPasswordUpdatedTime("helen"));

        // ...and the old password still logs in.
        var login = await client.PostAsJsonAsync(
            LoginUrl, new LoginRequest { UserId = "helen", Password = FakeAuthRepository.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_TooShort_RejectedWithComplexityMessage()
    {
        var (factory, client) = AuthenticatedAsHelen();
        using var _ = factory;

        // "Ab1!" satisfies 3 classes but is only 4 characters — fails the length rule.
        var response = await client.PostAsJsonAsync(
            ChangePasswordUrl, Body(FakeAuthRepository.Password, "Ab1!", "Ab1!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageBody>();
        Assert.Equal(PasswordPolicy.ComplexityMessage, body!.Message);
        Assert.Equal(FakeAuthRepository.InitialPasswordUpdatedTime, Fake(factory).GetPasswordUpdatedTime("helen"));
    }

    [Fact]
    public async Task ChangePassword_TooFewCharacterClasses_RejectedWithComplexityMessage()
    {
        var (factory, client) = AuthenticatedAsHelen();
        using var _ = factory;

        // 12 lowercase letters: long enough but only one character class (needs >= 3).
        var response = await client.PostAsJsonAsync(
            ChangePasswordUrl, Body(FakeAuthRepository.Password, "abcdefghijkl", "abcdefghijkl"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MessageBody>();
        Assert.Equal(PasswordPolicy.ComplexityMessage, body!.Message);
        Assert.Equal(FakeAuthRepository.InitialPasswordUpdatedTime, Fake(factory).GetPasswordUpdatedTime("helen"));
    }

    [Fact]
    public async Task ChangePassword_NewAndConfirmMismatch_Rejected()
    {
        var (factory, client) = AuthenticatedAsHelen();
        using var _ = factory;

        // Both are valid, complex passwords, but they don't match each other.
        var response = await client.PostAsJsonAsync(
            ChangePasswordUrl, Body(FakeAuthRepository.Password, "NewPass123", "NewPass124"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(FakeAuthRepository.InitialPasswordUpdatedTime, Fake(factory).GetPasswordUpdatedTime("helen"));
    }

    [Fact]
    public async Task ChangePassword_Valid_SetsHashToSha256OfNew_AndStampsUpdateTime()
    {
        var (factory, client) = AuthenticatedAsHelen();
        using var _ = factory;
        var fake = Fake(factory);

        const string newPassword = "NewPass123";
        var before = fake.GetPasswordUpdatedTime("helen");

        var response = await client.PostAsJsonAsync(
            ChangePasswordUrl, Body(FakeAuthRepository.Password, newPassword, newPassword));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // PasswordHash is exactly SHA-256(new)...
        Assert.Equal(PasswordHasher.Hash(newPassword), fake.GetPasswordHash("helen"));
        // ...and PasswordUpdatedTime advanced past the seed value.
        var after = fake.GetPasswordUpdatedTime("helen");
        Assert.NotNull(after);
        Assert.True(after > before, "PasswordUpdatedTime should move forward after a successful change.");

        // The new password logs in; the old one no longer does.
        var newLogin = await client.PostAsJsonAsync(
            LoginUrl, new LoginRequest { UserId = "helen", Password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);

        var oldLogin = await client.PostAsJsonAsync(
            LoginUrl, new LoginRequest { UserId = "helen", Password = FakeAuthRepository.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_Returns401()
    {
        var factory = new CmsApiFactory();
        using var _ = factory;
        var client = factory.CreateClient(); // no Authorization header

        var response = await client.PostAsJsonAsync(
            ChangePasswordUrl, Body("secret123", "NewPass123", "NewPass123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class MessageBody
    {
        public string Message { get; set; } = string.Empty;
    }
}
