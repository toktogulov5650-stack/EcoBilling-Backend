using System.IdentityModel.Tokens.Jwt;
using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.Authenticate;
using EcoBilling.Modules.Identity.Application.Tokens;
using EcoBilling.Modules.Identity.Contracts;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcoBilling.IntegrationTests.Api;

public sealed class UserAuthenticationConfigurationTests
{
    private const string SigningSecret =
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

    [Fact]
    public void AddUserAuthentication_WithValidKey_RegistersValidatedPolicies()
    {
        var services = new ServiceCollection();

        services.AddUserAuthentication(CreateConfiguration(SigningSecret));
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IAccessTokenIssuer>());
        Assert.Equal(5, provider.GetRequiredService<AuthenticationPolicy>().MaximumFailedAttempts);
        Assert.Equal(TimeSpan.FromDays(30), provider.GetRequiredService<TokenPolicy>().RefreshTokenLifetime);
    }

    [Fact]
    public void AddUserAuthentication_WithShortKey_RejectsConfiguration()
    {
        var services = new ServiceCollection();
        var shortKey = Convert.ToBase64String(new byte[16]);

        Assert.Throws<InvalidOperationException>(
            () => services.AddUserAuthentication(CreateConfiguration(shortKey)));
    }

    [Fact]
    public void AccessTokenIssuer_EmitsExpectedSubjectRoleLifetimeAndKeyId()
    {
        var services = new ServiceCollection();
        services.AddUserAuthentication(CreateConfiguration(SigningSecret));
        using var provider = services.BuildServiceProvider();
        var issuedAt = new DateTimeOffset(2026, 9, 28, 8, 0, 0, TimeSpan.Zero);
        var user = new AuthenticatedUser(new UserId(Guid.NewGuid()), UserRole.Director);

        var issued = provider.GetRequiredService<IAccessTokenIssuer>().Issue(user, issuedAt);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.Value);

        Assert.Equal("users-test-1", token.Header.Kid);
        Assert.Equal("https://api.ecobilling.test", token.Issuer);
        Assert.Contains("ecobilling-users-test", token.Audiences);
        Assert.Equal(user.UserId.Value.ToString("D"), token.Subject);
        Assert.Equal(UserRole.Director.ToString(), token.Claims.Single(claim => claim.Type == "role").Value);
        Assert.Equal(issuedAt.AddMinutes(15), issued.ExpiresAt);
        Assert.DoesNotContain(token.Claims, claim => claim.Type.Contains("refresh", StringComparison.OrdinalIgnoreCase));
    }

    private static IConfiguration CreateConfiguration(string signingSecret) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["UserAuthentication:Issuer"] = "https://api.ecobilling.test",
                    ["UserAuthentication:Audience"] = "ecobilling-users-test",
                    ["UserAuthentication:ActiveSigningKeyId"] = "users-test-1",
                    ["UserAuthentication:SigningKeys:0:KeyId"] = "users-test-1",
                    ["UserAuthentication:SigningKeys:0:SecretBase64"] = signingSecret,
                    ["UserAuthentication:AccessTokenLifetime"] = "00:15:00",
                    ["UserAuthentication:RefreshTokenLifetime"] = "30.00:00:00",
                    ["UserAuthentication:MaximumFailedAttempts"] = "5",
                    ["UserAuthentication:LockoutDuration"] = "00:15:00"
                })
            .Build();
}
