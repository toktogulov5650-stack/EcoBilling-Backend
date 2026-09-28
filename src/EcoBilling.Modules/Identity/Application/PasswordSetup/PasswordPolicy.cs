namespace EcoBilling.Modules.Identity.Application.PasswordSetup;

public sealed record PasswordPolicy
{
    public const int DefaultMinimumLength = 12;
    public const int DefaultMaximumLength = 256;

    public PasswordPolicy(int minimumLength, int maximumLength)
    {
        if (minimumLength < 1 || maximumLength < minimumLength)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumLength));
        }

        MinimumLength = minimumLength;
        MaximumLength = maximumLength;
    }

    public int MinimumLength { get; }

    public int MaximumLength { get; }

    public bool IsValid(string? password) =>
        !string.IsNullOrWhiteSpace(password) &&
        password.Length >= MinimumLength &&
        password.Length <= MaximumLength;

    public static PasswordPolicy Default { get; } = new(
        DefaultMinimumLength,
        DefaultMaximumLength);
}
