using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EcoBilling.Api.Configuration;
using EcoBilling.Api.InternalEndpoints;
using EcoBilling.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EcoBilling.IntegrationTests.Api;

public sealed class InternalDirectorProvisioningApiTests
{
    private const string Issuer = "https://control.ecobilling.test";
    private const string Audience = "ecobilling-district-test";
    private const string KeyId = "control-key-2026-09";
    private const string InitialCredential =
        "xTOHxQm8oC24bTWf9Uh5AF0w9Jm1mSIKzRFMtAlSHXo";

    [Fact]
    public void Request_ToStringRedactsPersonalDataAndCredential()
    {
        var request = new ProvisionDirectorRequest(
            "Ada Lovelace",
            "director@example.com",
            InitialCredential);

        var text = request.ToString();

        Assert.DoesNotContain("Ada Lovelace", text, StringComparison.Ordinal);
        Assert.DoesNotContain("director@example.com", text, StringComparison.Ordinal);
        Assert.DoesNotContain(InitialCredential, text, StringComparison.Ordinal);
        Assert.Contains("REDACTED", text, StringComparison.Ordinal);
    }

    [PostgreSqlFact]
    public async Task ProvisionDirector_WithValidServiceToken_CreatesDirector()
    {
        await using var host = await TestApiHost.CreateAsync();
        using var request = host.CreateRequest(
            "operation-1",
            host.CreateToken("token-1"));
        request.Headers.Add("X-Correlation-Id", "control-trace-1");

        using var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("false", response.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal("control-trace-1", response.Headers.GetValues("X-Correlation-Id").Single());
        var result = await response.Content.ReadFromJsonAsync<ProvisionDirectorResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.DirectorId);
        Assert.NotEqual(Guid.Empty, result.OperationId);
        Assert.Equal("created", result.Status);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(InitialCredential, responseBody, StringComparison.Ordinal);
    }

    [PostgreSqlFact]
    public async Task ProvisionDirector_RetryWithNewToken_ReturnsStoredResult()
    {
        await using var host = await TestApiHost.CreateAsync();
        using var firstRequest = host.CreateRequest(
            "operation-1",
            host.CreateToken("token-1"));
        using var firstResponse = await host.Client.SendAsync(firstRequest);
        var firstResult = await firstResponse.Content
            .ReadFromJsonAsync<ProvisionDirectorResponse>();
        using var replayRequest = host.CreateRequest(
            "operation-1",
            host.CreateToken("token-2"));

        using var replayResponse = await host.Client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.Created, replayResponse.StatusCode);
        Assert.Equal(
            "true",
            replayResponse.Headers.GetValues("Idempotency-Replayed").Single());
        var replayResult = await replayResponse.Content
            .ReadFromJsonAsync<ProvisionDirectorResponse>();
        Assert.Equal(firstResult, replayResult);
    }

    [PostgreSqlFact]
    public async Task ProvisionDirector_ReusedJwtIsRejectedBeforeOperationRunsAgain()
    {
        await using var host = await TestApiHost.CreateAsync();
        var token = host.CreateToken("token-1");
        using var firstRequest = host.CreateRequest("operation-1", token);
        using var firstResponse = await host.Client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        using var replayRequest = host.CreateRequest("operation-1", token);

        using var replayResponse = await host.Client.SendAsync(replayRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);
        await AssertProblemCodeAsync(replayResponse, "service.unauthorized");
    }

    [PostgreSqlFact]
    public async Task ProvisionDirector_SameIdempotencyKeyWithDifferentBody_ReturnsConflict()
    {
        await using var host = await TestApiHost.CreateAsync();
        using var firstRequest = host.CreateRequest(
            "operation-1",
            host.CreateToken("token-1"));
        using var firstResponse = await host.Client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        using var conflictingRequest = host.CreateRequest(
            "operation-1",
            host.CreateToken("token-2"),
            fullName: "Grace Hopper");

        using var conflictingResponse = await host.Client.SendAsync(conflictingRequest);

        Assert.Equal(HttpStatusCode.Conflict, conflictingResponse.StatusCode);
        await AssertProblemCodeAsync(
            conflictingResponse,
            "operation.idempotency_conflict");
    }

    [PostgreSqlFact]
    public async Task ProvisionDirector_WithoutServiceToken_ReturnsProblemDetails()
    {
        await using var host = await TestApiHost.CreateAsync();
        using var request = host.CreateRequest("operation-1", token: null);

        using var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemCodeAsync(response, "service.unauthorized");
    }

    [PostgreSqlFact]
    public async Task ProvisionDirector_WithWrongSigningKey_ReturnsProblemDetails()
    {
        await using var host = await TestApiHost.CreateAsync();
        using var otherRsa = RSA.Create(2048);
        var token = CreateToken(otherRsa, "token-1");
        using var request = host.CreateRequest("operation-1", token);

        using var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemCodeAsync(response, "service.unauthorized");
    }

    [PostgreSqlFact]
    public async Task ProvisionDirector_WithoutSigningKeyId_ReturnsProblemDetails()
    {
        await using var host = await TestApiHost.CreateAsync();
        var token = host.CreateToken("token-1", includeKeyId: false);
        using var request = host.CreateRequest("operation-1", token);

        using var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemCodeAsync(response, "service.unauthorized");
    }

    [PostgreSqlFact]
    public async Task ProvisionDirector_WithoutRequiredScope_ReturnsProblemDetails()
    {
        await using var host = await TestApiHost.CreateAsync();
        var token = host.CreateToken("token-1", scope: "another.scope");
        using var request = host.CreateRequest("operation-1", token);

        using var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemCodeAsync(response, "service.unauthorized");
    }

    [PostgreSqlFact]
    public async Task ProvisionDirector_WithExcessiveTokenLifetime_ReturnsProblemDetails()
    {
        await using var host = await TestApiHost.CreateAsync();
        var token = host.CreateToken("token-1", lifetime: TimeSpan.FromMinutes(3));
        using var request = host.CreateRequest("operation-1", token);

        using var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertProblemCodeAsync(response, "service.unauthorized");
    }

    [PostgreSqlFact]
    public async Task ProvisionDirector_WithoutIdempotencyKey_ReturnsValidationProblem()
    {
        await using var host = await TestApiHost.CreateAsync();
        using var request = host.CreateRequest(
            idempotencyKey: null,
            host.CreateToken("token-1"));

        using var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, "operation.invalid_idempotency_key");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("validationErrors", out _));
    }

    [PostgreSqlFact]
    public async Task ProvisionDirector_WithoutBody_ReturnsValidationProblem()
    {
        await using var host = await TestApiHost.CreateAsync();
        using var request = host.CreateRequest(
            "operation-1",
            host.CreateToken("token-1"));
        request.Content = null;

        using var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, "validation.failed");
    }

    [PostgreSqlFact]
    public async Task ProvisionDirector_WithMalformedJson_ReturnsValidationProblem()
    {
        await using var host = await TestApiHost.CreateAsync();
        using var request = host.CreateRequest(
            "operation-1",
            host.CreateToken("token-1"));
        request.Content = new StringContent("{", Encoding.UTF8, "application/json");

        using var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemCodeAsync(response, "validation.failed");
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            expectedCode,
            document.RootElement.GetProperty("code").GetString());
        Assert.False(
            string.IsNullOrWhiteSpace(
                document.RootElement.GetProperty("traceId").GetString()));
    }

    private static string CreateToken(
        RSA rsa,
        string tokenId,
        string scope = InternalServiceAuthenticationOptions.RequiredScope,
        TimeSpan? lifetime = null,
        bool includeKeyId = true)
    {
        var now = DateTimeOffset.UtcNow;
        var securityKey = new RsaSecurityKey(rsa)
        {
            KeyId = includeKeyId ? KeyId : null
        };
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "ecobilling-control"),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
            new Claim("scope", scope)
        };
        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            notBefore: now.UtcDateTime,
            expires: now.Add(lifetime ?? TimeSpan.FromMinutes(1)).UtcDateTime,
            signingCredentials: new SigningCredentials(
                securityKey,
                SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class TestApiHost : IAsyncDisposable
    {
        private readonly PostgreSqlTestDatabase database;
        private readonly RSA rsa;
        private readonly InternalApiFactory factory;
        private readonly TestEnvironment environment;

        private TestApiHost(
            PostgreSqlTestDatabase database,
            RSA rsa,
            InternalApiFactory factory,
            TestEnvironment environment,
            HttpClient client)
        {
            this.database = database;
            this.rsa = rsa;
            this.factory = factory;
            this.environment = environment;
            Client = client;
        }

        public HttpClient Client { get; }

        public static async Task<TestApiHost> CreateAsync()
        {
            var database = await PostgreSqlTestDatabase.CreateAsync();
            await using (var context = database.CreateContext())
            {
                await context.Database.MigrateAsync();
            }

            var rsa = RSA.Create(2048);
            var environment = new TestEnvironment(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings__EcoBilling"] = database.ConnectionString,
                    ["DirectorProvisioning__RequestFingerprintKey"] =
                        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=",
                    ["InternalServiceAuthentication__Issuer"] = Issuer,
                    ["InternalServiceAuthentication__Audience"] = Audience,
                    ["InternalServiceAuthentication__MaximumTokenLifetime"] = "00:02:00",
                    ["InternalServiceAuthentication__ClockSkew"] = "00:00:30",
                    ["InternalServiceAuthentication__SigningKeys__0__KeyId"] = KeyId,
                    ["InternalServiceAuthentication__SigningKeys__0__PublicKeyPem"] =
                        rsa.ExportSubjectPublicKeyInfoPem()
                });

            try
            {
                var factory = new InternalApiFactory();
                var client = factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        BaseAddress = new Uri("https://localhost")
                    });
                return new TestApiHost(database, rsa, factory, environment, client);
            }
            catch
            {
                environment.Dispose();
                rsa.Dispose();
                await database.DisposeAsync();
                throw;
            }
        }

        public string CreateToken(
            string tokenId,
            string scope = InternalServiceAuthenticationOptions.RequiredScope,
            TimeSpan? lifetime = null,
            bool includeKeyId = true) =>
            InternalDirectorProvisioningApiTests.CreateToken(
                rsa,
                tokenId,
                scope,
                lifetime,
                includeKeyId);

        public HttpRequestMessage CreateRequest(
            string? idempotencyKey,
            string? token,
            string fullName = "Ada Lovelace")
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/internal/v1/directors")
            {
                Content = JsonContent.Create(
                    new ProvisionDirectorRequest(
                        fullName,
                        "director@example.com",
                        InitialCredential))
            };
            if (idempotencyKey is not null)
            {
                request.Headers.Add(
                    DirectorProvisioningEndpoints.IdempotencyKeyHeaderName,
                    idempotencyKey);
            }

            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    token);
            }

            return request;
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await factory.DisposeAsync();
            environment.Dispose();
            rsa.Dispose();
            await database.DisposeAsync();
        }
    }

    private sealed class InternalApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
        }
    }

    private sealed class TestEnvironment : IDisposable
    {
        private readonly Dictionary<string, string?> originalValues;

        public TestEnvironment(IReadOnlyDictionary<string, string?> values)
        {
            originalValues = values.Keys.ToDictionary(
                key => key,
                Environment.GetEnvironmentVariable,
                StringComparer.Ordinal);

            foreach (var (key, value) in values)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        public void Dispose()
        {
            foreach (var (key, value) in originalValues)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
