using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using EcoBilling.Api.Configuration;
using EcoBilling.Api.InternalEndpoints;
using EcoBilling.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EcoBilling.EndToEndTests.Infrastructure;

internal sealed class EcoBillingEndToEndHost : IAsyncDisposable
{
    internal const string InitialCredential =
        "xTOHxQm8oC24bTWf9Uh5AF0w9Jm1mSIKzRFMtAlSHXo";

    private const string Issuer = "https://control.ecobilling.e2e";
    private const string Audience = "ecobilling-district-e2e";
    private const string KeyId = "control-e2e-key-1";
    private readonly PostgreSqlEndToEndDatabase database;
    private readonly RSA rsa;
    private readonly EndToEndApiFactory factory;

    private EcoBillingEndToEndHost(
        PostgreSqlEndToEndDatabase database,
        RSA rsa,
        EndToEndApiFactory factory,
        HttpClient client)
    {
        this.database = database;
        this.rsa = rsa;
        this.factory = factory;
        Client = client;
    }

    public HttpClient Client { get; }

    public IReadOnlyCollection<string> RoutePatterns =>
        factory.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
            .ToArray();

    public static async Task<EcoBillingEndToEndHost> CreateAsync()
    {
        var database = await PostgreSqlEndToEndDatabase.CreateAsync();
        RSA? rsa = null;
        EndToEndApiFactory? factory = null;

        try
        {
            await using (var context = database.CreateContext())
            {
                await context.Database.MigrateAsync();
            }

            rsa = RSA.Create(2048);
            factory = new EndToEndApiFactory(
                database.ConnectionString,
                rsa.ExportSubjectPublicKeyInfoPem());
            var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    BaseAddress = new Uri("https://localhost")
                });
            return new EcoBillingEndToEndHost(database, rsa, factory, client);
        }
        catch
        {
            if (factory is not null)
            {
                await factory.DisposeAsync();
            }

            rsa?.Dispose();
            await database.DisposeAsync();
            throw;
        }
    }

    public EcoBillingDbContext CreateContext() => database.CreateContext();

    public HttpRequestMessage CreateDirectorRequest(
        string idempotencyKey,
        string? token)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/internal/v1/directors")
        {
            Content = JsonContent.Create(
                new ProvisionDirectorRequest(
                    "Ada Lovelace",
                    "director@example.com",
                    InitialCredential))
        };
        request.Headers.Add(
            DirectorProvisioningEndpoints.IdempotencyKeyHeaderName,
            idempotencyKey);

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                token);
        }

        return request;
    }

    public string CreateServiceToken(string tokenId)
    {
        var now = DateTimeOffset.UtcNow;
        var securityKey = new RsaSecurityKey(rsa)
        {
            KeyId = KeyId
        };
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "ecobilling-control"),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
            new Claim(
                "scope",
                InternalServiceAuthenticationOptions.RequiredScope)
        };
        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(1).UtcDateTime,
            signingCredentials: new SigningCredentials(
                securityKey,
                SecurityAlgorithms.RsaSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await factory.DisposeAsync();
        rsa.Dispose();
        await database.DisposeAsync();
    }

    private sealed class EndToEndApiFactory(
        string connectionString,
        string publicKeyPem)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:EcoBilling",
                connectionString);
            builder.UseSetting(
                "DirectorProvisioning:RequestFingerprintKey",
                "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");
            builder.UseSetting(
                "InternalServiceAuthentication:Issuer",
                Issuer);
            builder.UseSetting(
                "InternalServiceAuthentication:Audience",
                Audience);
            builder.UseSetting(
                "InternalServiceAuthentication:MaximumTokenLifetime",
                "00:02:00");
            builder.UseSetting(
                "InternalServiceAuthentication:ClockSkew",
                "00:00:30");
            builder.UseSetting(
                "InternalServiceAuthentication:SigningKeys:0:KeyId",
                KeyId);
            builder.UseSetting(
                "InternalServiceAuthentication:SigningKeys:0:PublicKeyPem",
                publicKeyPem);
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
