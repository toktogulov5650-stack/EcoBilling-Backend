namespace EcoBilling.Modules.Identity.Application.Tokens;

public sealed record TokenPolicy
{
    public static readonly TimeSpan DefaultRefreshTokenLifetime = TimeSpan.FromDays(30);

    public TokenPolicy(TimeSpan refreshTokenLifetime)
    {
        if (refreshTokenLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshTokenLifetime));
        }

        RefreshTokenLifetime = refreshTokenLifetime;
    }

    public TimeSpan RefreshTokenLifetime { get; }

    public static TokenPolicy Default { get; } = new(DefaultRefreshTokenLifetime);
}
