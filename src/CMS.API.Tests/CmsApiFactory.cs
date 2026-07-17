using System.Net.Http.Headers;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Services;
using CMS.API.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CMS.API.Tests;

/// <summary>
/// Boots the real API pipeline but swaps the Dapper repository for an in-memory fake,
/// so tests hit real routing / serialization without needing SQL Server.
/// </summary>
public sealed class CmsApiFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Runs after the fakes above are registered, so a test can replace one of them (or add a
    /// service of its own) for a single case — e.g. swapping in a repository that throws to
    /// exercise the global exception middleware. Left null by the CRUD suites.
    /// </summary>
    public Action<IServiceCollection>? CustomizeServices { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAppRoleRepository>();
            services.AddSingleton<IAppRoleRepository, FakeAppRoleRepository>();

            services.RemoveAll<IAppUserRepository>();
            services.AddSingleton<IAppUserRepository, FakeAppUserRepository>();

            services.RemoveAll<IPublishStatusRepository>();
            services.AddSingleton<IPublishStatusRepository, FakePublishStatusRepository>();

            services.RemoveAll<IPartnerRepository>();
            services.AddSingleton<IPartnerRepository, FakePartnerRepository>();

            services.RemoveAll<ICourseGroupRepository>();
            services.AddSingleton<ICourseGroupRepository, FakeCourseGroupRepository>();

            services.RemoveAll<ICourseRepository>();
            services.AddSingleton<ICourseRepository, FakeCourseRepository>();

            services.RemoveAll<IFeaturedPromoItemRepository>();
            services.AddSingleton<IFeaturedPromoItemRepository, FakeFeaturedPromoItemRepository>();

            services.RemoveAll<IRowAuditRepository>();
            services.AddSingleton<IRowAuditRepository, FakeRowAuditRepository>();

            services.RemoveAll<ILookupRepository>();
            services.AddSingleton<ILookupRepository, FakeLookupRepository>();

            services.RemoveAll<IAuthRepository>();
            services.AddSingleton<IAuthRepository, FakeAuthRepository>();

            CustomizeServices?.Invoke(services);
        });
    }

    /// <summary>
    /// A client carrying a valid Bearer token (signed with <see cref="FakeAuthRepository.SigningSecret"/>,
    /// the same key the JwtBearer middleware validates against). Used by the CRUD test suites, whose
    /// endpoints are now protected by the global authorization policy.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(params string[] roles)
        => CreateAuthenticatedClientAs("tester", "Test User", roles);

    /// <summary>
    /// Like <see cref="CreateAuthenticatedClient"/> but with an explicit <paramref name="userId"/> /
    /// <paramref name="userName"/> in the token — used by profile tests that must target a specific
    /// seeded user (the endpoint reads the acting user from the JWT, not the request body).
    /// </summary>
    public HttpClient CreateAuthenticatedClientAs(string userId, string userName, params string[] roles)
    {
        var token = new JwtTokenService().CreateToken(
            new LoginCredential
            {
                UserId = userId,
                UserName = userName,
                IsActive = true,
                RoleIds = [.. roles],
            },
            FakeAuthRepository.SigningSecret);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
