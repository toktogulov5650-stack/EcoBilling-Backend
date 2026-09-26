namespace EcoBilling.Modules.Identity.Application.ProvisionDirector;

public sealed class ProvisionDirectorCommand
{
    public ProvisionDirectorCommand(
        string? idempotencyKey,
        string? fullName,
        string? email,
        string? initialCredential)
    {
        IdempotencyKey = idempotencyKey;
        FullName = fullName;
        Email = email;
        InitialCredential = initialCredential;
    }

    public string? IdempotencyKey { get; }

    public string? FullName { get; }

    public string? Email { get; }

    public string? InitialCredential { get; }

    public override string ToString() =>
        $"{nameof(ProvisionDirectorCommand)} {{ IdempotencyKey = [REDACTED], FullName = [REDACTED], Email = [REDACTED], InitialCredential = [REDACTED] }}";
}
