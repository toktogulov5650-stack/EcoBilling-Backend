using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using EcoBilling.Api.Endpoints;
using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Infrastructure.Persistence;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.IntegrationTests.Api;

public sealed class UserAuthenticationApiTests
{
    private const string Password = "correct-test-password";
    private const string InitialCredential = "initial-credential-for-director";
    private const string ReplacementPassword = "replacement-password";

    [PostgreSqlFact]
    public async Task Login_ReturnsSignedAccessTokenAndStoresOnlyRefreshHash()
    {
        await using var host = await UserAuthHost.CreateAsync();

        using var response = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("email", "controller@example.com", Password));
        var tokenPair = await response.Content.ReadFromJsonAsync<TokenPairResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(tokenPair);
        Assert.Equal("Bearer", tokenPair.TokenType);
        Assert.Equal(UserRole.Controller.ToString(), tokenPair.Role);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenPair.AccessToken);
        Assert.Equal(host.UserId.Value.ToString("D"), jwt.Subject);
        Assert.Equal("users-test-1", jwt.Header.Kid);

        await using var context = host.CreateContext();
        var session = await context.RefreshSessions.AsNoTracking().SingleAsync();
        Assert.Equal(host.UserId, session.UserId);
        Assert.Equal(new RefreshTokenService().Hash(tokenPair.RefreshToken), session.TokenHash);
        Assert.DoesNotContain(tokenPair.RefreshToken, session.TokenHash, StringComparison.Ordinal);
    }

    [PostgreSqlFact]
    public async Task Refresh_RotatesTokenAndReplayRevokesReplacementFamily()
    {
        await using var host = await UserAuthHost.CreateAsync();
        var login = await LoginAsync(host.Client);

        using var refreshResponse = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(login.RefreshToken));
        var rotated = await refreshResponse.Content.ReadFromJsonAsync<TokenPairResponse>();
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.NotNull(rotated);
        Assert.NotEqual(login.RefreshToken, rotated.RefreshToken);

        using var replayResponse = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(login.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);
        Assert.Equal(
            "auth.invalid_refresh_token",
            await ReadProblemCodeAsync(replayResponse));

        using var replacementResponse = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(rotated.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, replacementResponse.StatusCode);

        await using var context = host.CreateContext();
        Assert.All(
            await context.RefreshSessions.AsNoTracking().ToArrayAsync(),
            session => Assert.NotNull(session.RevokedAt));
    }

    [PostgreSqlFact]
    public async Task Login_AfterFiveFailuresRemainsUnauthorizedWithCorrectPassword()
    {
        await using var host = await UserAuthHost.CreateAsync();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await host.Client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest("email", "controller@example.com", "wrong-password"));
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        using var locked = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("email", "controller@example.com", Password));

        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        await using var context = host.CreateContext();
        var account = await context.UserAccounts.AsNoTracking().SingleAsync();
        Assert.Equal(5, account.FailedLoginAttempts);
        Assert.NotNull(account.LockoutEnd);
    }

    [PostgreSqlFact]
    public async Task Revoke_IsIdempotentAndInvalidatesRefreshFamily()
    {
        await using var host = await UserAuthHost.CreateAsync();
        var login = await LoginAsync(host.Client);

        using var revoke = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/revoke",
            new RefreshTokenRequest(login.RefreshToken));
        using var repeatedRevoke = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/revoke",
            new RefreshTokenRequest(login.RefreshToken));
        using var refresh = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(login.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, repeatedRevoke.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [PostgreSqlFact]
    public async Task SetupPassword_ReplacesInitialCredentialBeforeFirstLogin()
    {
        await using var host = await UserAuthHost.CreateAsync(
            UserRole.Director,
            requiresPasswordChange: true,
            InitialCredential);

        using var blockedLogin = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("email", "controller@example.com", InitialCredential));
        using var setup = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/setup-password",
            new SetInitialPasswordRequest(
                "email",
                "controller@example.com",
                InitialCredential,
                ReplacementPassword));
        using var login = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("email", "controller@example.com", ReplacementPassword));
        using var repeatedSetup = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/setup-password",
            new SetInitialPasswordRequest(
                "email",
                "controller@example.com",
                InitialCredential,
                ReplacementPassword));

        Assert.Equal(HttpStatusCode.Forbidden, blockedLogin.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, setup.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, repeatedSetup.StatusCode);
    }

    private static async Task<TokenPairResponse> LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("email", "controller@example.com", Password));
        response.EnsureSuccessStatusCode();
        return Assert.IsType<TokenPairResponse>(
            await response.Content.ReadFromJsonAsync<TokenPairResponse>());
    }

    private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("code").GetString();
    }

    private sealed class UserAuthHost : IAsyncDisposable
    {
        private readonly PostgreSqlTestDatabase database;
        private readonly RSA rsa;
        private readonly UserAuthApiFactory factory;

        private UserAuthHost(
            PostgreSqlTestDatabase database,
            RSA rsa,
            UserAuthApiFactory factory,
            HttpClient client,
            UserId userId)
        {
            this.database = database;
            this.rsa = rsa;
            this.factory = factory;
            Client = client;
            UserId = userId;
        }

        public HttpClient Client { get; }

        public UserId UserId { get; }

        public EcoBillingDbContext CreateContext() => database.CreateContext();

        public static async Task<UserAuthHost> CreateAsync(
            UserRole role = UserRole.Controller,
            bool requiresPasswordChange = false,
            string password = Password)
        {
            var database = await PostgreSqlTestDatabase.CreateAsync();
            var rsa = RSA.Create(2048);
            var userId = new UserId(Guid.NewGuid());
            try
            {
                await using (var context = database.CreateContext())
                {
                    await context.Database.MigrateAsync();
                    var account = UserAccount.Create(
                        userId,
                        LoginIdentity.Create(LoginType.Email, "controller@example.com").Value,
                        new PasswordHasherAdapter().Hash(password),
                        role,
                        DateTimeOffset.UtcNow,
                        requiresPasswordChange).Value;
                    context.UserAccounts.Add(account);
                    await context.SaveChangesAsync();
                }

                var factory = new UserAuthApiFactory(
                    database.ConnectionString,
                    rsa.ExportSubjectPublicKeyInfoPem());
                var client = factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        BaseAddress = new Uri("https://localhost")
                    });
                return new UserAuthHost(database, rsa, factory, client, userId);
            }
            catch
            {
                rsa.Dispose();
                await database.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await factory.DisposeAsync();
            rsa.Dispose();
            await database.DisposeAsync();
        }
    }

    private sealed class UserAuthApiFactory(
        string connectionString,
        string publicKeyPem)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:EcoBilling", connectionString);
            builder.UseSetting(
                "DirectorProvisioning:RequestFingerprintKey",
                "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=");
            builder.UseSetting("InternalServiceAuthentication:Issuer", "https://control.ecobilling.test");
            builder.UseSetting("InternalServiceAuthentication:Audience", "ecobilling-test");
            builder.UseSetting("InternalServiceAuthentication:SigningKeys:0:KeyId", "control-test-1");
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
