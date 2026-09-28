namespace EcoBilling.Modules.Identity.Application.Authenticate;

public sealed record AuthenticationPolicy
{
    public const int DefaultMaximumFailedAttempts = 5;
    public static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(15);

    public AuthenticationPolicy(int maximumFailedAttempts, TimeSpan lockoutDuration)
    {
        if (maximumFailedAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumFailedAttempts));
        }

        if (lockoutDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lockoutDuration));
        }

        MaximumFailedAttempts = maximumFailedAttempts;
        LockoutDuration = lockoutDuration;
    }

    public int MaximumFailedAttempts { get; }

    public TimeSpan LockoutDuration { get; }

    public static AuthenticationPolicy Default { get; } = new(
        DefaultMaximumFailedAttempts,
        DefaultLockoutDuration);
}
