using EcoBilling.Modules.Controllers.Domain;

namespace EcoBilling.Modules.Controllers.Features.CreateController;

public sealed record CreateControllerResult(
    ControllerId ControllerId,
    ControllerCreationOperationId OperationId,
    bool IsReplay);
