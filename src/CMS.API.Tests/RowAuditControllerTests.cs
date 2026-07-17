using System.Net;
using System.Net.Http.Json;
using CMS.API.Models;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end API tests for the reusable audit-history endpoint (<c>GET /api/rowaudit</c>) running against
/// an in-memory fake repository. Verifies it filters by tableName + pkid and returns rows newest first.
/// </summary>
public class RowAuditControllerTests
{
    private static (CmsApiFactory factory, HttpClient client) CreateClient()
    {
        var factory = new CmsApiFactory();
        return (factory, factory.CreateAuthenticatedClient());
    }

    [Fact]
    public async Task Get_FiltersByTableNameAndPkid_AndReturnsNewestFirst()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var entries = await client.GetFromJsonAsync<List<RowAuditEntry>>("/api/rowaudit?tableName=Course&pkid=123");

        Assert.NotNull(entries);
        // Only the three Course/123 rows — not Course/456, not Partner/123.
        Assert.Equal(3, entries!.Count);
        Assert.All(entries, e => Assert.Contains(e.UserName, new[] { "alice", "bob", "carol" }));

        // Newest first: 2026-06-04 (alice) > 2026-06-02 (carol) > 2026-06-01 (bob).
        Assert.Equal(new[] { "alice", "carol", "bob" }, entries.Select(e => e.UserName).ToArray());
        Assert.True(entries[0].DateTime >= entries[1].DateTime);
        Assert.True(entries[1].DateTime >= entries[2].DateTime);

        // The projection carries the four display fields.
        var latest = entries[0];
        Assert.Equal("Update", latest.ActionType);
        Assert.Equal("Title, Description", latest.ActionDesc);
        Assert.Equal(new DateTime(2026, 6, 4, 14, 30, 0), latest.DateTime);
    }

    [Fact]
    public async Task Get_DifferentTableSamePkid_IsExcluded()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        // Partner/123 exists in the fake; a Course/123 query must not surface it.
        var courseEntries = await client.GetFromJsonAsync<List<RowAuditEntry>>("/api/rowaudit?tableName=Course&pkid=123");
        Assert.DoesNotContain(courseEntries!, e => e.UserName == "erin");

        var partnerEntries = await client.GetFromJsonAsync<List<RowAuditEntry>>("/api/rowaudit?tableName=Partner&pkid=123");
        Assert.Single(partnerEntries!);
        Assert.Equal("erin", partnerEntries!.Single().UserName);
        Assert.Equal("Delete", partnerEntries!.Single().ActionType);
    }

    [Fact]
    public async Task Get_RecordWithNoHistory_ReturnsEmptyList()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var entries = await client.GetFromJsonAsync<List<RowAuditEntry>>("/api/rowaudit?tableName=Course&pkid=999");

        Assert.NotNull(entries);
        Assert.Empty(entries!);
    }

    [Fact]
    public async Task Get_MissingTableName_Returns400()
    {
        var (factory, client) = CreateClient();
        using var _ = factory;

        var response = await client.GetAsync("/api/rowaudit?pkid=123");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        var factory = new CmsApiFactory();
        using var _ = factory;
        var anon = factory.CreateClient();

        var response = await anon.GetAsync("/api/rowaudit?tableName=Course&pkid=123");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
