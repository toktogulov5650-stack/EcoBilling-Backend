using EcoBilling.Modules.Controllers.Contracts;
using EcoBilling.Modules.Controllers.Domain;

namespace EcoBilling.Modules.Controllers.Features.Abstractions;

public interface IControllerAssignmentRepository
{
    Task<ControllerAssignmentPersistenceResult> AssignAsync(
        ControllerAssignment assignment,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken);

    Task<ControllerAssignmentRemovalOutcome> RemoveAsync(
        ControllerId controllerId,
        ControllerAssignmentId assignmentId,
        string actorId,
        string correlationId,
        DateTimeOffset removedAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ControllerAssignmentDetails>> ListByControllerIdAsync(
        ControllerId controllerId,
        CancellationToken cancellationToken);
}

public enum ControllerAssignmentRemovalOutcome
{
    Removed = 0,
    NotFound = 1
}
