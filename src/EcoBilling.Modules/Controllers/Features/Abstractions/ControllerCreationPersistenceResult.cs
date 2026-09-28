using EcoBilling.Modules.Controllers.Domain;

namespace EcoBilling.Modules.Controllers.Features.Abstractions;

public sealed record ControllerCreationPersistenceResult(
    ControllerCreationPersistenceOutcome Outcome,
    ControllerId? ControllerId = null,
    ControllerCreationOperationId? OperationId = null);

public enum ControllerCreationPersistenceOutcome
{
    Created = 1,
    Replayed = 2,
    IdempotencyConflict = 3,
    EmailAlreadyExists = 4
}
