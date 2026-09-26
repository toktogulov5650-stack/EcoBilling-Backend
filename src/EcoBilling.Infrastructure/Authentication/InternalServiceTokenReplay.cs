namespace EcoBilling.Infrastructure.Authentication;

internal sealed class InternalServiceTokenReplay
{
    public const int MaximumIssuerLength = 200;
    public const int MaximumTokenIdLength = 200;

    private InternalServiceTokenReplay()
    {
        Issuer = string.Empty;
        TokenId = string.Empty;
    }

    public InternalServiceTokenReplay(
        string issuer,
        string tokenId,
        DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenId);

        if (issuer.Length > MaximumIssuerLength)
        {
            throw new ArgumentOutOfRangeException(nameof(issuer));
        }

        if (tokenId.Length > MaximumTokenIdLength)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenId));
        }

        Issuer = issuer;
        TokenId = tokenId;
        ExpiresAt = expiresAt.ToUniversalTime();
    }

    public string Issuer { get; private set; }

    public string TokenId { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }
}
