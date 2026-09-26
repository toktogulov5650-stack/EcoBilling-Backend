namespace EcoBilling.Infrastructure.Authentication;

public interface IInternalServiceTokenReplayStore
{
    Task<bool> TryConsumeAsync(
        string issuer,
        string tokenId,
        DateTimeOffset expiresAt,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);
}
