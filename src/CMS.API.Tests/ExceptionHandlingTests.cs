using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CMS.API.Tests;

/// <summary>
/// End-to-end tests for the global exception middleware: an endpoint whose repository throws must
/// answer with one generic 500 JSON body that leaks nothing, while the responses that already carry
/// meaning — 401, 403, and validation 400s — keep behaving exactly as they did before.
/// </summary>
public class ExceptionHandlingTests
{
    /// <summary>A factory whose PublishStatus repository throws on every call.</summary>
    private static CmsApiFactory CreateThrowingFactory(RecordingLoggerProvider? logs = null)
        => new()
        {
            CustomizeServices = services =>
            {
                services.RemoveAll<IPublishStatusRepository>();
                services.AddSingleton<IPublishStatusRepository, ThrowingPublishStatusRepository>();

                if (logs is not null)
                {
                    services.AddSingleton<ILoggerProvider>(logs);
                }
            },
        };

    // --- Unexpected exceptions become one generic 500 ------------------------

    [Fact]
    public async Task ThrowingEndpoint_Returns500WithGenericMessage()
    {
        using var factory = CreateThrowingFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/publish-statuses");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(ErrorResponse.GenericMessage, body!.Message);
        Assert.False(string.IsNullOrWhiteSpace(body.TraceId));
    }

    [Fact]
    public async Task ThrowingEndpoint_ResponseLeaksNoExceptionDetail()
    {
        using var factory = CreateThrowingFactory();
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/publish-statuses");
        var body = await response.Content.ReadAsStringAsync();

        // Nothing about how the failure happened may cross the wire.
        Assert.DoesNotContain(ThrowingPublishStatusRepository.SqlFragment, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ThrowingPublishStatusRepository.ConnectionFragment, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ThrowingPublishStatusRepository", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);

        // The body carries the generic message and a trace id, and nothing else.
        using var json = JsonDocument.Parse(body);
        Assert.Equal(
            ["message", "traceId"],
            json.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("GET", "/api/publish-statuses")]
    [InlineData("GET", "/api/publish-statuses/1")]
    [InlineData("POST", "/api/publish-statuses/query")]
    [InlineData("DELETE", "/api/publish-statuses/1")]
    public async Task ThrowingEndpoint_EveryVerbGetsTheSameShape(string method, string url)
    {
        using var factory = CreateThrowingFactory();
        var client = factory.CreateAuthenticatedClient();

        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new PublishStatusQuery());
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(ErrorResponse.GenericMessage, body!.Message);
    }

    [Fact]
    public async Task ThrowingEndpoint_LogsFullExceptionServerSide()
    {
        var logs = new RecordingLoggerProvider();
        using var factory = CreateThrowingFactory(logs);
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/publish-statuses");
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        var entry = Assert.Single(
            logs.Entries,
            e => e.Level == LogLevel.Error && e.Exception is InvalidOperationException);

        // The detail withheld from the client is present in full for the operator...
        Assert.Equal(ThrowingPublishStatusRepository.FailureMessage, entry.Exception!.Message);
        Assert.False(string.IsNullOrEmpty(entry.Exception.StackTrace));

        // ...and the trace id the client was given points at this entry.
        Assert.Contains(body!.TraceId, entry.Message, StringComparison.Ordinal);
    }

    // --- Responses that already mean something are untouched -----------------

    [Fact]
    public async Task Unauthenticated_Still401_NotSwallowedInto500()
    {
        using var factory = CreateThrowingFactory();
        var client = factory.CreateClient();

        // No token: the auth challenge must win before the throwing repository is ever reached.
        var response = await client.GetAsync("/api/publish-statuses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.SingleOrDefault()?.Scheme);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Forbidden_Still403_NotSwallowedInto500()
    {
        using var factory = new CmsApiFactory();

        // A valid token without the Admin role: [Authorize(Roles = "Admin")] must still forbid.
        var client = factory.CreateAuthenticatedClient("Viewer");

        var response = await client.PostAsync("/api/appusers/helen/reset-password", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ValidationError_Still400ProblemDetails_NotSwallowedInto500()
    {
        using var factory = new CmsApiFactory();
        var client = factory.CreateAuthenticatedClient();

        // Description is [Required] — the automatic ModelState 400 must survive unchanged.
        var response = await client.PostAsJsonAsync(
            "/api/publish-statuses",
            new PublishStatusRequest { Pkid = 5, Description = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Description", out _));
        Assert.DoesNotContain(ErrorResponse.GenericMessage, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ControllerMessageError_Still400WithItsOwnMessage()
    {
        using var factory = new CmsApiFactory();
        var client = factory.CreateAuthenticatedClient();

        // RowAuditController's own { message } 400 must not be replaced by the generic one.
        var response = await client.GetAsync("/api/rowaudit");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("tableName", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ErrorResponse.GenericMessage, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HealthyEndpoint_StillReturns200()
    {
        using var factory = new CmsApiFactory();
        var client = factory.CreateAuthenticatedClient();

        // The middleware must be invisible on the happy path.
        var response = await client.GetAsync("/api/publish-statuses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
