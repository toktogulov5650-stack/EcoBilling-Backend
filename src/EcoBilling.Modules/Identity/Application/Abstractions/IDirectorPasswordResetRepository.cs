using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Application.Abstractions;

public interface IDirectorPasswordResetRepository
{
    Task<DirectorPasswordResetPersistenceResult> ResetAsync(
        DirectorPasswordResetOperation operation,
        string newPasswordHash,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken);
}

public enum DirectorPasswordResetPersistenceOutcome
{
    Reset = 1,
    Replayed = 2,
    IdempotencyConflict = 3,
    DirectorNotFound = 4
}

public sealed record DirectorPasswordResetPersistenceResult(
    DirectorPasswordResetPersistenceOutcome Outcome,
    DirectorPasswordResetOperationId? OperationId = null);
