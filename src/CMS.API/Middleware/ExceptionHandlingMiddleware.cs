using System.Diagnostics;
using CMS.API.Models;

namespace CMS.API.Middleware;

/// <summary>
/// Catches every exception escaping the rest of the pipeline — controller, repository, Dapper —
/// logs it in full server-side, and replies with one generic <see cref="ErrorResponse"/> at 500.
/// Nothing about the failure (stack trace, SQL text, connection string) reaches the client.
/// </summary>
/// <remarks>
/// Registered first in <c>Program.cs</c> so it wraps the whole pipeline. It only sees *exceptions*:
/// 401 challenges, 403 forbids, and 400 validation responses are produced by their own middleware
/// or by MVC without throwing, so they pass through untouched.
/// </remarks>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (IsClientDisconnect(context, ex))
        {
            // The client hung up mid-request. Nobody is left to answer, and an aborted request is
            // not a server fault, so let it unwind quietly rather than logging it as an error.
        }
        catch (Exception ex)
        {
            // Activity.Current.Id is the W3C traceparent the logging provider also stamps on the
            // entry, so the id echoed to the client finds this exception in the log.
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            logger.LogError(
                ex,
                "Unhandled exception for {Method} {Path} (traceId {TraceId}).",
                context.Request.Method,
                context.Request.Path.Value,
                traceId);

            // Headers are already on the wire, so the status line can no longer be changed.
            // Rethrow and let the server tear the connection down rather than append a JSON body
            // to a half-written response.
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteGenericErrorAsync(context, traceId);
        }
    }

    private static bool IsClientDisconnect(HttpContext context, Exception ex)
        => ex is OperationCanceledException && context.RequestAborted.IsCancellationRequested;

    private static Task WriteGenericErrorAsync(HttpContext context, string traceId)
    {
        // Drop anything a partially-run handler had already staged. CORS headers survive: the CORS
        // middleware applies them from an OnStarting callback, which Clear() does not remove.
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // CancellationToken.None: the body must be written even if the request is being aborted,
        // otherwise the client sees a truncated response instead of the error.
        return context.Response.WriteAsJsonAsync(
            new ErrorResponse(ErrorResponse.GenericMessage, traceId),
            CancellationToken.None);
    }
}
