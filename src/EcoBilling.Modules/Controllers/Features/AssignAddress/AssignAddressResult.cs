using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Controllers.Domain;

namespace EcoBilling.Modules.Controllers.Features.AssignAddress;

public sealed record AssignAddressResult(
    ControllerAssignmentId AssignmentId,
    ControllerId ControllerId,
    AddressId AddressId,
    DateTimeOffset CreatedAt);
