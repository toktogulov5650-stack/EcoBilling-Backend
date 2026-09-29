using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace EcoBilling.Api.Configuration;

public static class ReverseProxyExtensions
{
    public static IServiceCollection AddEcoBillingReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = configuration
            .GetSection(ReverseProxyOptions.SectionName)
            .Get<ReverseProxyOptions>()
            ?? new ReverseProxyOptions();

        if (settings.Enabled)
        {
            if (settings.ForwardLimit < 1 ||
                settings.KnownProxies.Count == 0 ||
                settings.KnownProxies.Any(
                    proxy => !IPAddress.TryParse(proxy, out _)))
            {
                throw new InvalidOperationException(
                    "ReverseProxy configuration requires ForwardLimit >= 1 and only explicit IP addresses in KnownProxies when enabled.");
            }

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = settings.ForwardLimit;
                options.KnownProxies.Clear();

                foreach (var proxy in settings.KnownProxies)
                {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }
            });
        }

        services.AddSingleton(settings);
        return services;
    }

    public static WebApplication UseEcoBillingReverseProxy(
        this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var settings = app.Services.GetRequiredService<ReverseProxyOptions>();
        if (settings.Enabled)
        {
            app.UseForwardedHeaders();
        }

        return app;
    }
}
