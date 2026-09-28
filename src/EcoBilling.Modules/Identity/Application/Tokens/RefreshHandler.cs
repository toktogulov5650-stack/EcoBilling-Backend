using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Contracts;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Application.Tokens;

public sealed class RefreshHandler
{
    private readonly IAccessTokenIssuer accessTokenIssuer;
    private readonly IRefreshTokenService refreshTokenService;
    private readonly IRefreshSessionRepository refreshSessionRepository;
    private readonly TokenPolicy tokenPolicy;
    private readonly TimeProvider timeProvider;

    public RefreshHandler(
        IAccessTokenIssuer accessTokenIssuer,
        IRefreshTokenService refreshTokenService,
        IRefreshSessionRepository refreshSessionRepository,
        TokenPolicy tokenPolicy,
        TimeProvider timeProvider)
    {
        this.accessTokenIssuer = accessTokenIssuer;
        this.refreshTokenService = refreshTokenService;
        this.refreshSessionRepository = refreshSessionRepository;
        this.tokenPolicy = tokenPolicy;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<TokenPairResult>> Handle(
        string? presentedRefreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(presentedRefreshToken))
        {
            return Result<TokenPairResult>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        var now = timeProvider.GetUtcNow();
        var generated = refreshTokenService.Generate();
        var replacementExpiresAt = now.Add(tokenPolicy.RefreshTokenLifetime);

        var rotation = await refreshSessionRepository.RotateAsync(
            refreshTokenService.Hash(presentedRefreshToken),
            new RefreshSessionId(Guid.NewGuid()),
            generated.Hash,
            now,
            replacementExpiresAt,
            cancellationToken);
        if (rotation.Outcome is not RefreshSessionRotationOutcome.Rotated ||
            rotation.UserId is null ||
            rotation.Role is null)
        {
            return Result<TokenPairResult>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        var user = new AuthenticatedUser(rotation.UserId, rotation.Role.Value);
        var accessToken = accessTokenIssuer.Issue(user, now);

        return Result<TokenPairResult>.Success(
            new TokenPairResult(
                user,
                accessToken.Value,
                accessToken.ExpiresAt,
                generated.Value,
                replacementExpiresAt));
    }
}
