using EcoBilling.Modules.Controllers.Domain;

namespace EcoBilling.Modules.Controllers.Features.Abstractions;

public sealed record ControllerAssignmentPersistenceResult(
    ControllerAssignmentPersistenceOutcome Outcome,
    ControllerAssignmentId? AssignmentId = null,
    DateTimeOffset? CreatedAt = null);

public enum ControllerAssignmentPersistenceOutcome
{
    Assigned = 0,
    Replayed = 1,
    ControllerNotFound = 2,
    AddressNotFound = 3
}
