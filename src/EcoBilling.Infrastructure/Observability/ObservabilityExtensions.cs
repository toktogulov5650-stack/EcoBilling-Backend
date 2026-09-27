using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EcoBilling.Infrastructure.Observability;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddEcoBillingObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        bool instrumentAspNetCore)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var otlpEndpoint = GetOtlpEndpoint(configuration);
        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(EcoBillingTelemetry.WorkerActivitySourceName)
                    .AddHttpClientInstrumentation()
                    .AddNpgsql();

                if (instrumentAspNetCore)
                {
                    tracing.AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health/live");
                    });
                }

                if (otlpEndpoint is not null)
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = otlpEndpoint);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(EcoBillingTelemetry.WorkerMeterName)
                    .AddNpgsqlInstrumentation(_ => { })
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (instrumentAspNetCore)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }

                if (otlpEndpoint is not null)
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = otlpEndpoint);
                }
            });

        return services;
    }

    public static ILoggingBuilder AddEcoBillingStructuredLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(logging);
        ArgumentNullException.ThrowIfNull(configuration);

        var otlpEndpoint = GetOtlpEndpoint(configuration);

        logging.ClearProviders();
        logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            options.UseUtcTimestamp = true;
        });
        logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;

            if (otlpEndpoint is not null)
            {
                options.AddOtlpExporter(exporter => exporter.Endpoint = otlpEndpoint);
            }
        });

        return logging;
    }

    private static Uri? GetOtlpEndpoint(IConfiguration configuration)
    {
        var configuredEndpoint = configuration[
            EcoBillingTelemetry.OtlpEndpointConfigurationKey];
        if (string.IsNullOrWhiteSpace(configuredEndpoint))
        {
            return null;
        }

        if (!Uri.TryCreate(configuredEndpoint, UriKind.Absolute, out var endpoint) ||
            (endpoint.Scheme != Uri.UriSchemeHttp &&
             endpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"Configuration '{EcoBillingTelemetry.OtlpEndpointConfigurationKey}' must be an absolute HTTP or HTTPS URI.");
        }

        return endpoint;
    }
}
