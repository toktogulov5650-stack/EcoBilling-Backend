namespace EcoBilling.Modules.Identity.Application.ProvisionDirector;

public sealed class ProvisionDirectorCommand
{
    public ProvisionDirectorCommand(
        string? idempotencyKey,
        string? fullName,
        string? email,
        string? initialCredential,
        string actorId,
        string correlationId)
    {
        IdempotencyKey = idempotencyKey;
        FullName = fullName;
        Email = email;
        InitialCredential = initialCredential;
        ActorId = actorId;
        CorrelationId = correlationId;
    }

    public string? IdempotencyKey { get; }

    public string? FullName { get; }

    public string? Email { get; }

    public string? InitialCredential { get; }

    public string ActorId { get; }

    public string CorrelationId { get; }

    public override string ToString() =>
        $"{nameof(ProvisionDirectorCommand)} {{ IdempotencyKey = [REDACTED], FullName = [REDACTED], Email = [REDACTED], InitialCredential = [REDACTED] }}";
}
