using System.Security.Cryptography;
using EcoBilling.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcoBilling.IntegrationTests.Api;

public sealed class InternalServiceAuthenticationConfigurationTests
{
    [Fact]
    public void AddInternalServiceAuthentication_WithPublicKey_Succeeds()
    {
        using var rsa = RSA.Create(2048);
        var configuration = CreateConfiguration(
            rsa.ExportSubjectPublicKeyInfoPem());
        var services = new ServiceCollection();

        services.AddInternalServiceAuthentication(configuration);
    }

    [Fact]
    public void AddInternalServiceAuthentication_WithPrivateKey_RejectsConfiguration()
    {
        using var rsa = RSA.Create(2048);
        var configuration = CreateConfiguration(rsa.ExportPkcs8PrivateKeyPem());
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddInternalServiceAuthentication(configuration));

        Assert.Contains("public RSA key", exception.Message, StringComparison.Ordinal);
    }

    private static IConfiguration CreateConfiguration(string pem) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["InternalServiceAuthentication:Issuer"] =
                        "https://control.ecobilling.test",
                    ["InternalServiceAuthentication:Audience"] =
                        "ecobilling-district-test",
                    ["InternalServiceAuthentication:SigningKeys:0:KeyId"] =
                        "control-key-1",
                    ["InternalServiceAuthentication:SigningKeys:0:PublicKeyPem"] = pem
                })
            .Build();
}
