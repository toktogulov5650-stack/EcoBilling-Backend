using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.Modules.Residents.Features.Abstractions;

public sealed record ResidentPasswordResetPersistenceResult(
    ResidentPasswordResetPersistenceOutcome Outcome,
    ResidentPasswordResetOperationId? OperationId = null);

public enum ResidentPasswordResetPersistenceOutcome
{
    Reset = 1,
    Replayed = 2,
    IdempotencyConflict = 3,
    ResidentNotFound = 4
}
