using EcoBilling.Modules.Identity.Application.Abstractions;

namespace EcoBilling.Modules.Identity.Application.Tokens;

public sealed class RevokeRefreshTokenHandler(
    IRefreshTokenService refreshTokenService,
    IRefreshSessionRepository refreshSessionRepository,
    TimeProvider timeProvider)
{
    public async Task Handle(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        await refreshSessionRepository.RevokeAsync(
            refreshTokenService.Hash(refreshToken),
            timeProvider.GetUtcNow(),
            cancellationToken);
    }
}
