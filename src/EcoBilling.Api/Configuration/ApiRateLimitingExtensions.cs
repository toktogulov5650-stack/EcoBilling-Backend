using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace EcoBilling.Api.Configuration;

public static class ApiRateLimitingExtensions
{
    public static IServiceCollection AddEcoBillingRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<ApiRateLimitingOptions>()
            .Bind(configuration.GetSection(ApiRateLimitingOptions.SectionName))
            .Validate(
                options => options.AuthenticationPermitLimit > 0,
                "Authentication rate-limit permit limit must be positive.")
            .Validate(
                options => options.AuthenticationWindow > TimeSpan.Zero,
                "Authentication rate-limit window must be positive.")
            .Validate(
                options => options.AuthenticationQueueLimit >= 0,
                "Authentication rate-limit queue limit cannot be negative.")
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = static async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                httpContext.Response.ContentType = "application/problem+json";

                var retryAfter = context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter,
                    out var value)
                    ? value
                    : (TimeSpan?)null;

                if (retryAfter is not null)
                {
                    httpContext.Response.Headers["Retry-After"] =
                        Math.Max(1, (int)Math.Ceiling(retryAfter.Value.TotalSeconds))
                            .ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                await httpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        title = "Too many requests",
                        status = StatusCodes.Status429TooManyRequests,
                        detail = "Too many authentication requests were received. Try again later.",
                        instance = httpContext.Request.Path.Value,
                        code = "rate_limit.exceeded",
                        traceId = httpContext.TraceIdentifier
                    },
                    cancellationToken);
            };

            options.AddPolicy(
                ApiRateLimitingOptions.AuthenticationPolicy,
                httpContext =>
                {
                    var settings = httpContext.RequestServices
                        .GetRequiredService<IOptions<ApiRateLimitingOptions>>()
                        .Value;

                    var partitionKey =
                        httpContext.Connection.RemoteIpAddress?.ToString() ??
                        "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.AuthenticationPermitLimit,
                            Window = settings.AuthenticationWindow,
                            QueueLimit = settings.AuthenticationQueueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true
                        });
                });
        });

        return services;
    }
}
