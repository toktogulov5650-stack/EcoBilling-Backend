using EcoBilling.Modules.Controllers.Domain;

namespace EcoBilling.Modules.Controllers.Features.RemoveAssignment;

public sealed record RemoveAssignmentCommand(
    ControllerId ControllerId,
    ControllerAssignmentId AssignmentId,
    string ActorId,
    string CorrelationId);
