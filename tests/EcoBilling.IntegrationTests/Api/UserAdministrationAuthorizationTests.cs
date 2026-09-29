using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace EcoBilling.IntegrationTests.Api;

public sealed class UserAdministrationAuthorizationTests
{
    private const string Issuer = "https://api.ecobilling.test";
    private const string Audience = "ecobilling-users-test";
    private const string KeyId = "users-test-1";
    private const string SecretBase64 =
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

    [Fact]
    public async Task RevokeAllSessions_WithoutAccessToken_ReturnsUnauthorizedProblem()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(ClientOptions());

        using var response = await client.PostAsync(
            $"/api/v1/users/{Guid.NewGuid():D}/sessions/revoke-all",
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemCodeAsync(response, "auth.unauthorized");
    }

    [Theory]
    [InlineData(UserRole.Controller)]
    [InlineData(UserRole.Resident)]
    public async Task RevokeAllSessions_WithNonDirectorRole_ReturnsForbiddenProblem(
        UserRole role)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(ClientOptions());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateAccessToken(role));

        using var response = await client.PostAsync(
            $"/api/v1/users/{Guid.NewGuid():D}/sessions/revoke-all",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertProblemCodeAsync(response, "auth.forbidden");
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(
            expectedCode,
            document.RootElement.GetProperty("code").GetString());
    }

    private static string CreateAccessToken(UserRole role)
    {
        var signingKey = new SymmetricSecurityKey(
            Convert.FromBase64String(SecretBase64))
        {
            KeyId = KeyId
        };
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString("D")),
                new Claim("role", role.ToString())
            ],
            now.AddMinutes(-1),
            now.AddMinutes(5),
            new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
        token.Header[JwtHeaderParameterNames.Kid] = KeyId;
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserAdministrationApiFactory CreateFactory()
    {
        using var rsa = RSA.Create(2048);
        return new UserAdministrationApiFactory(
            rsa.ExportSubjectPublicKeyInfoPem());
    }

    private static WebApplicationFactoryClientOptions ClientOptions() =>
        new()
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        };

    private sealed class UserAdministrationApiFactory(string publicKeyPem)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:EcoBilling",
                "Host=127.0.0.1;Port=1;Database=unavailable;Username=api;Password=api-test;Timeout=1;Command Timeout=1;Pooling=false");
            builder.UseSetting(
                "DirectorProvisioning:RequestFingerprintKey",
                SecretBase64);
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
            builder.UseSetting("UserAuthentication:Issuer", Issuer);
            builder.UseSetting("UserAuthentication:Audience", Audience);
            builder.UseSetting("UserAuthentication:ActiveSigningKeyId", KeyId);
            builder.UseSetting("UserAuthentication:SigningKeys:0:KeyId", KeyId);
            builder.UseSetting(
                "UserAuthentication:SigningKeys:0:SecretBase64",
                SecretBase64);
        }
    }
}
