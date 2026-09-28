using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EcoBilling.IntegrationTests.Api;

public sealed class SwaggerEndpointsTests
{
    [Fact]
    public async Task Swagger_InDevelopment_ExposesUiAndBearerContract()
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient(ClientOptions());

        using var uiResponse = await client.GetAsync("/swagger/index.html");
        using var contractResponse = await client.GetAsync(
            "/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, uiResponse.StatusCode);
        Assert.Contains(
            "EcoBilling API",
            await uiResponse.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, contractResponse.StatusCode);

        using var document = JsonDocument.Parse(
            await contractResponse.Content.ReadAsStringAsync());
        Assert.True(
            document.RootElement
                .GetProperty("paths")
                .TryGetProperty("/api/v1/auth/login", out _));

        var bearer = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
    }

    [Fact]
    public async Task Swagger_OutsideDevelopment_IsNotPublished()
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient(ClientOptions());

        using var uiResponse = await client.GetAsync("/swagger/index.html");
        using var contractResponse = await client.GetAsync(
            "/swagger/v1/swagger.json");
        using var nativeContractResponse = await client.GetAsync(
            "/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, uiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, contractResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, nativeContractResponse.StatusCode);
    }

    private static SwaggerApiFactory CreateFactory(string environment)
    {
        using var rsa = RSA.Create(2048);
        return new SwaggerApiFactory(
            environment,
            rsa.ExportSubjectPublicKeyInfoPem());
    }

    private static WebApplicationFactoryClientOptions ClientOptions() =>
        new()
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        };

    private sealed class SwaggerApiFactory(
        string environment,
        string publicKeyPem)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.UseSetting(
                "ConnectionStrings:EcoBilling",
                "Host=127.0.0.1;Port=1;Database=unavailable;Username=swagger;Password=swagger-test;Timeout=1;Command Timeout=1;Pooling=false");
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
            builder.UseSetting(
                "UserAuthentication:Issuer",
                "https://api.ecobilling.test");
            builder.UseSetting(
                "UserAuthentication:Audience",
                "ecobilling-users-test");
            builder.UseSetting(
                "UserAuthentication:ActiveSigningKeyId",
                "users-test-1");
            builder.UseSetting(
                "UserAuthentication:SigningKeys:0:KeyId",
                "users-test-1");
            builder.UseSetting(
                "UserAuthentication:SigningKeys:0:SecretBase64",
                "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");
        }
    }
}
