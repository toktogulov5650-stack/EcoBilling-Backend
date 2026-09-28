using EcoBilling.Modules.Identity.Contracts;

namespace EcoBilling.Modules.Identity.Application.Tokens;

public sealed record TokenPairResult(
    AuthenticatedUser User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public override string ToString() =>
        $"{nameof(TokenPairResult)} {{ User = {User}, AccessToken = [REDACTED], RefreshToken = [REDACTED] }}";
}
