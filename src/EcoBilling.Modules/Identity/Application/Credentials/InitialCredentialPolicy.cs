namespace EcoBilling.Modules.Identity.Application.Credentials;

public static class InitialCredentialPolicy
{
    public const int MinimumLength = 32;
    public const int MaximumLength = 256;

    public static bool IsValid(string? credential) =>
        !string.IsNullOrWhiteSpace(credential) &&
        credential.Length >= MinimumLength &&
        credential.Length <= MaximumLength &&
        !credential.Any(char.IsWhiteSpace);
}
