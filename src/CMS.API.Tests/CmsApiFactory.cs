using CMS.API.Repositories;
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
        });
    }
}
