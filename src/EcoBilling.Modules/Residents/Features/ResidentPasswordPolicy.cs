namespace EcoBilling.Modules.Residents.Features;

public static class ResidentPasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 256;

    public static bool IsValid(string? password) =>
        !string.IsNullOrWhiteSpace(password) &&
        password.Length >= MinimumLength &&
        password.Length <= MaximumLength &&
        !password.Any(char.IsWhiteSpace);
}
