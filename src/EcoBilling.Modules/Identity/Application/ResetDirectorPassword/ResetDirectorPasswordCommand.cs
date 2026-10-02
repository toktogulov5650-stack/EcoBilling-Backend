namespace EcoBilling.Modules.Identity.Application.ResetDirectorPassword;

public sealed record ResetDirectorPasswordCommand(
    string? IdempotencyKey,
    string? Email,
    string? NewPassword,
    string ActorId,
    string CorrelationId)
{
    public override string ToString() =>
        $"{nameof(ResetDirectorPasswordCommand)} {{ IdempotencyKey = [REDACTED], Email = [REDACTED], NewPassword = [REDACTED], ActorId = [REDACTED], CorrelationId = [REDACTED] }}";
}
