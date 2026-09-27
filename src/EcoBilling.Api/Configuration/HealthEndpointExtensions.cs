using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EcoBilling.Api.Configuration;

public static class HealthEndpointExtensions
{
    public static IEndpointRouteBuilder MapEcoBillingHealthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks(
                "/health/live",
                CreateOptions(registration => registration.Tags.Contains("live")))
            .AllowAnonymous()
            .ExcludeFromDescription();
        endpoints.MapHealthChecks(
                "/health/ready",
                CreateOptions(registration => registration.Tags.Contains("ready")))
            .AllowAnonymous()
            .ExcludeFromDescription();
        endpoints.MapHealthChecks(
                "/health",
                CreateOptions(registration => registration.Tags.Contains("ready")))
            .AllowAnonymous()
            .ExcludeFromDescription();

        return endpoints;
    }

    private static HealthCheckOptions CreateOptions(
        Func<HealthCheckRegistration, bool> predicate) =>
        new()
        {
            Predicate = predicate,
            AllowCachingResponses = false,
            ResponseWriter = WriteResponseAsync
        };

    private static Task WriteResponseAsync(
        HttpContext context,
        HealthReport report)
    {
        var response = new HealthResponse(
            report.Status.ToString(),
            report.TotalDuration.TotalMilliseconds,
            report.Entries
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new HealthCheckResponse(
                    entry.Key,
                    entry.Value.Status.ToString(),
                    entry.Value.Duration.TotalMilliseconds))
                .ToArray());

        return context.Response.WriteAsJsonAsync(
            response,
            cancellationToken: context.RequestAborted);
    }

    private sealed record HealthResponse(
        string Status,
        double DurationMilliseconds,
        IReadOnlyCollection<HealthCheckResponse> Checks);

    private sealed record HealthCheckResponse(
        string Name,
        string Status,
        double DurationMilliseconds);
}
