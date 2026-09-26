using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Application.Abstractions;

public sealed record DirectorProvisioningPersistenceResult(
    DirectorProvisioningPersistenceOutcome Outcome,
    DirectorId? DirectorId = null,
    DirectorProvisioningOperationId? OperationId = null);

public enum DirectorProvisioningPersistenceOutcome
{
    Created = 1,
    Replayed = 2,
    IdempotencyConflict = 3,
    DirectorAlreadyExists = 4
}
