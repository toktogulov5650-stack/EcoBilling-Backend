using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.Authenticate;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Application.Tokens;

public sealed class LoginHandler
{
    private readonly AuthenticateHandler authenticateHandler;
    private readonly IAccessTokenIssuer accessTokenIssuer;
    private readonly IRefreshTokenService refreshTokenService;
    private readonly IRefreshSessionRepository refreshSessionRepository;
    private readonly TokenPolicy tokenPolicy;
    private readonly TimeProvider timeProvider;

    public LoginHandler(
        AuthenticateHandler authenticateHandler,
        IAccessTokenIssuer accessTokenIssuer,
        IRefreshTokenService refreshTokenService,
        IRefreshSessionRepository refreshSessionRepository,
        TokenPolicy tokenPolicy,
        TimeProvider timeProvider)
    {
        this.authenticateHandler = authenticateHandler;
        this.accessTokenIssuer = accessTokenIssuer;
        this.refreshTokenService = refreshTokenService;
        this.refreshSessionRepository = refreshSessionRepository;
        this.tokenPolicy = tokenPolicy;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<TokenPairResult>> Handle(
        AuthenticateCommand command,
        CancellationToken cancellationToken)
    {
        var authentication = await authenticateHandler.Handle(command, cancellationToken);
        if (authentication.IsFailure)
        {
            return Result<TokenPairResult>.Failure(authentication.Error);
        }

        var now = timeProvider.GetUtcNow();
        var refreshToken = refreshTokenService.Generate();
        var refreshExpiresAt = now.Add(tokenPolicy.RefreshTokenLifetime);
        var session = RefreshSession.Create(
            new RefreshSessionId(Guid.NewGuid()),
            authentication.Value.User.UserId,
            Guid.NewGuid(),
            refreshToken.Hash,
            now,
            refreshExpiresAt);
        if (session.IsFailure)
        {
            throw new InvalidOperationException("The refresh token service produced an invalid token hash.");
        }

        await refreshSessionRepository.CreateAsync(session.Value, cancellationToken);
        var accessToken = accessTokenIssuer.Issue(authentication.Value.User, now);

        return Result<TokenPairResult>.Success(
            new TokenPairResult(
                authentication.Value.User,
                accessToken.Value,
                accessToken.ExpiresAt,
                refreshToken.Value,
                refreshExpiresAt));
    }
}
