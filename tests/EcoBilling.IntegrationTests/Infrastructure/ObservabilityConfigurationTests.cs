using EcoBilling.Infrastructure.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcoBilling.IntegrationTests.Infrastructure;

public sealed class ObservabilityConfigurationTests
{
    [Fact]
    public void AddEcoBillingObservability_WithoutExporter_RegistersSuccessfully()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddEcoBillingObservability(
            configuration,
            "EcoBilling.Test",
            instrumentAspNetCore: false);

        using var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider);
    }

    [Theory]
    [InlineData("relative/path")]
    [InlineData("ftp://collector.example.test")]
    public void AddEcoBillingObservability_WithInvalidEndpoint_RejectsConfiguration(
        string endpoint)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [EcoBillingTelemetry.OtlpEndpointConfigurationKey] = endpoint
                })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddEcoBillingObservability(
                configuration,
                "EcoBilling.Test",
                instrumentAspNetCore: false));

        Assert.Contains(
            EcoBillingTelemetry.OtlpEndpointConfigurationKey,
            exception.Message,
            StringComparison.Ordinal);
    }
}
