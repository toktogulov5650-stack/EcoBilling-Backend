using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EcoBilling.IntegrationTests.Api;

public sealed class HealthEndpointsTests
{
    [Fact]
    public async Task LiveEndpoint_WhenPostgreSqlIsUnavailable_ReturnsHealthy()
    {
        using var rsa = RSA.Create(2048);
        await using var factory = new HealthApiFactory(
            rsa.ExportSubjectPublicKeyInfoPem());
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-Id", "health-live-test");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "health-live-test",
            response.Headers.GetValues("X-Correlation-Id").Single());
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
        var check = Assert.Single(
            document.RootElement.GetProperty("checks").EnumerateArray());
        Assert.Equal("self", check.GetProperty("name").GetString());
        Assert.Equal("Healthy", check.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ReadyEndpoint_WhenPostgreSqlIsUnavailable_ReturnsSanitizedFailure()
    {
        using var rsa = RSA.Create(2048);
        await using var factory = new HealthApiFactory(
            rsa.ExportSubjectPublicKeyInfoPem());
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(
            HealthApiFactory.DatabasePassword,
            body,
            StringComparison.Ordinal);
        Assert.DoesNotContain("NpgsqlException", body, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("Unhealthy", document.RootElement.GetProperty("status").GetString());
        var check = Assert.Single(
            document.RootElement.GetProperty("checks").EnumerateArray());
        Assert.Equal("postgresql", check.GetProperty("name").GetString());
        Assert.Equal("Unhealthy", check.GetProperty("status").GetString());
    }

    [Fact]
    public async Task LiveEndpoint_WithOversizedCorrelationId_GeneratesSafeIdentifier()
    {
        using var rsa = RSA.Create(2048);
        await using var factory = new HealthApiFactory(
            rsa.ExportSubjectPublicKeyInfoPem());
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
        var oversizedCorrelationId = new string('a', 129);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-Id", oversizedCorrelationId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returnedCorrelationId = response.Headers
            .GetValues("X-Correlation-Id")
            .Single();
        Assert.NotEqual(oversizedCorrelationId, returnedCorrelationId);
        Assert.False(string.IsNullOrWhiteSpace(returnedCorrelationId));
    }

    [Fact]
    public async Task LiveEndpoint_WithUntrustedHost_ReturnsBadRequest()
    {
        using var rsa = RSA.Create(2048);
        await using var factory = new HealthApiFactory(
            rsa.ExportSubjectPublicKeyInfoPem());
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Host = "untrusted.example";

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class HealthApiFactory(string publicKeyPem)
        : WebApplicationFactory<Program>
    {
        public const string DatabasePassword = "health-check-secret";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:EcoBilling",
                $"Host=127.0.0.1;Port=1;Database=unavailable;Username=health;Password={DatabasePassword};Timeout=1;Command Timeout=1;Pooling=false");
            builder.UseSetting(
                "DirectorProvisioning:RequestFingerprintKey",
                "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");
            builder.UseSetting(
                "InternalServiceAuthentication:Issuer",
                "https://control.ecobilling.test");
            builder.UseSetting(
                "InternalServiceAuthentication:Audience",
                "ecobilling-district-test");
            builder.UseSetting(
                "InternalServiceAuthentication:SigningKeys:0:KeyId",
                "control-key-1");
            builder.UseSetting(
                "InternalServiceAuthentication:SigningKeys:0:PublicKeyPem",
                publicKeyPem);
            ConfigureUserAuthentication(builder);
        }

        private static void ConfigureUserAuthentication(IWebHostBuilder builder)
        {
            builder.UseSetting("UserAuthentication:Issuer", "https://api.ecobilling.test");
            builder.UseSetting("UserAuthentication:Audience", "ecobilling-users-test");
            builder.UseSetting("UserAuthentication:ActiveSigningKeyId", "users-test-1");
            builder.UseSetting("UserAuthentication:SigningKeys:0:KeyId", "users-test-1");
            builder.UseSetting(
                "UserAuthentication:SigningKeys:0:SecretBase64",
                "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");
        }
    }
}
