using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.Modules.Residents.Features.ResetPassword;

public sealed class ResetResidentPasswordCommand
{
    public ResetResidentPasswordCommand(
        ResidentId residentId,
        string? idempotencyKey,
        string? newPassword,
        string actorId,
        string correlationId)
    {
        ResidentId = residentId ?? throw new ArgumentNullException(nameof(residentId));
        IdempotencyKey = idempotencyKey;
        NewPassword = newPassword;
        ActorId = actorId;
        CorrelationId = correlationId;
    }

    public ResidentId ResidentId { get; }

    public string? IdempotencyKey { get; }

    public string? NewPassword { get; }

    public string ActorId { get; }

    public string CorrelationId { get; }

    public override string ToString() =>
        $"{nameof(ResetResidentPasswordCommand)} {{ ResidentId = {ResidentId.Value:D}, IdempotencyKey = [REDACTED], NewPassword = [REDACTED], ActorId = [REDACTED], CorrelationId = [REDACTED] }}";
}
