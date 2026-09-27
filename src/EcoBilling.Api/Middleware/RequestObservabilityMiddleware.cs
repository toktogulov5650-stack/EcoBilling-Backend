using System.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace EcoBilling.Api.Middleware;

public sealed class RequestObservabilityMiddleware(
    RequestDelegate next,
    ILogger<RequestObservabilityMiddleware> logger,
    TimeProvider timeProvider)
{
    private static readonly EventId RequestCompletedEvent = new(
        1000,
        "HttpRequestCompleted");

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var startedAt = timeProvider.GetTimestamp();
        var activity = Activity.Current;
        activity?.SetTag("ecobilling.correlation_id", context.TraceIdentifier);
        var traceId = activity?.TraceId.ToHexString() ?? context.TraceIdentifier;

        using var scope = logger.BeginScope(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["CorrelationId"] = context.TraceIdentifier,
                ["TraceId"] = traceId
            });

        await next(context);

        var elapsed = timeProvider.GetElapsedTime(startedAt);
        var endpoint = context.GetEndpoint() is RouteEndpoint routeEndpoint
            ? routeEndpoint.RoutePattern.RawText ?? routeEndpoint.DisplayName
            : context.GetEndpoint()?.DisplayName;
        var userId = context.User.FindFirst("sub")?.Value;
        var role = context.User.FindFirst("role")?.Value;
        var logLevel = context.Response.StatusCode switch
        {
            >= StatusCodes.Status500InternalServerError => LogLevel.Error,
            >= StatusCodes.Status400BadRequest => LogLevel.Warning,
            _ => LogLevel.Information
        };

        logger.Log(
            logLevel,
            RequestCompletedEvent,
            "HTTP {HttpRequestMethod} {HttpEndpoint} responded {HttpResponseStatusCode} in {HttpRequestDurationMilliseconds} ms for user {UserId} with role {UserRole}",
            context.Request.Method,
            endpoint ?? "unmatched",
            context.Response.StatusCode,
            elapsed.TotalMilliseconds,
            userId,
            role);
    }
}
