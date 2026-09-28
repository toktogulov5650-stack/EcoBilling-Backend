using EcoBilling.Modules.Identity.Contracts;

namespace EcoBilling.Modules.Identity.Application.Abstractions;

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(AuthenticatedUser user, DateTimeOffset issuedAt);
}

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAt);
