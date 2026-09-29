using System.Net;
using EcoBilling.Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EcoBilling.IntegrationTests.Api;

public sealed class ReverseProxyConfigurationTests
{
    [Fact]
    public void DisabledByDefault_DoesNotTrustForwardedHeaders()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddEcoBillingReverseProxy(configuration);
        using var provider = services.BuildServiceProvider();

        var settings = provider.GetRequiredService<ReverseProxyOptions>();
        Assert.False(settings.Enabled);
    }

    [Fact]
    public void EnabledWithoutKnownProxy_RejectsConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(
            enabled: true,
            knownProxy: null);

        Assert.Throws<InvalidOperationException>(
            () => services.AddEcoBillingReverseProxy(configuration));
    }

    [Fact]
    public void EnabledWithExactKnownProxy_ConfiguresForwardedHeaders()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(
            enabled: true,
            knownProxy: "10.0.0.10");

        services.AddEcoBillingReverseProxy(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;

        Assert.Equal(1, options.ForwardLimit);
        Assert.Contains(IPAddress.Parse("10.0.0.10"), options.KnownProxies);
        Assert.True(
            options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedFor));
        Assert.True(
            options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedProto));
    }

    private static IConfiguration CreateConfiguration(
        bool enabled,
        string? knownProxy) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ReverseProxy:Enabled"] = enabled.ToString(),
                    ["ReverseProxy:ForwardLimit"] = "1",
                    ["ReverseProxy:KnownProxies:0"] = knownProxy
                })
            .Build();
}
