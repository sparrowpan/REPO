using System.Net;
using System.Net.Http.Json;
using CMS.API.Repositories;
using CMS.API.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CMS.API.Tests;

/// <summary>
/// Regression: ISSUE-002 — deleting a still-referenced record answered 500, not 409.
/// Found by /qa on 2026-07-17.
/// Report: .gstack/qa-reports/qa-report-localhost-4200-2026-07-17.md
///
/// A PublishStatus / Partner / CourseGroup still assigned to a Course let SQL Server's foreign key
/// violation escape to the exception middleware, so the caller got the generic
/// "系統發生錯誤，請稍後再試。" 500 with no way to tell a routine refusal from a real outage. The
/// repositories now translate error 547 into <see cref="CMS.API.Infrastructure.ReferencedRecordException"/>
/// and the controllers answer 409 naming the entity.
///
/// These pin the controller's half of that contract: the refusal is a 409 carrying a usable message,
/// deletes that are not refused still succeed, and a missing row is still a 404 rather than a 409.
/// </summary>
public class PublishStatusesDeleteReferencedTests
{
    private static (CmsApiFactory factory, HttpClient client) CreateClient()
    {
        var factory = new CmsApiFactory
        {
            CustomizeServices = services =>
            {
                services.RemoveAll<IPublishStatusRepository>();
                services.AddSingleton<IPublishStatusRepository, ReferencedPublishStatusRepository>();
            },
        };
        return (factory, factory.CreateAuthenticatedClient());
    }

    [Fact]
    public async Task Delete_StillReferencedByCourses_Returns409()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync($"/api/publish-statuses/{ReferencedPublishStatusRepository.ReferencedPkid}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_StillReferencedByCourses_ExplainsWhyInsteadOfTheGenericFailure()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync($"/api/publish-statuses/{ReferencedPublishStatusRepository.ReferencedPkid}");
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        var message = Assert.Contains("message", body!);
        Assert.Equal("此發布狀態已被課程使用，無法刪除。", message);
        // The whole point of the 409: the caller must not be told this was an unexpected server fault.
        Assert.DoesNotContain("系統發生錯誤", message);
    }

    [Fact]
    public async Task Delete_NotReferenced_StillSucceeds()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/publish-statuses/2");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_MissingRow_IsStillNotFoundNotConflict()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.DeleteAsync("/api/publish-statuses/200");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
