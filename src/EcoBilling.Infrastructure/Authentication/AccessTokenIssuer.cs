using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Contracts;
using Microsoft.IdentityModel.Tokens;

namespace EcoBilling.Infrastructure.Authentication;

public sealed class AccessTokenIssuer : IAccessTokenIssuer
{
    private readonly string issuer;
    private readonly string audience;
    private readonly TimeSpan lifetime;
    private readonly SigningCredentials signingCredentials;

    public AccessTokenIssuer(
        string issuer,
        string audience,
        TimeSpan lifetime,
        SymmetricSecurityKey signingKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        ArgumentNullException.ThrowIfNull(signingKey);
        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        }

        this.issuer = issuer;
        this.audience = audience;
        this.lifetime = lifetime;
        signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);
    }

    public IssuedAccessToken Issue(AuthenticatedUser user, DateTimeOffset issuedAt)
    {
        ArgumentNullException.ThrowIfNull(user);
        var utcIssuedAt = issuedAt.ToUniversalTime();
        var expiresAt = utcIssuedAt.Add(lifetime);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.Value.ToString("D")),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D")),
                new Claim(
                    JwtRegisteredClaimNames.Iat,
                    utcIssuedAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ClaimValueTypes.Integer64),
                new Claim("role", user.Role.ToString())
            ],
            notBefore: utcIssuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials);

        return new IssuedAccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}
