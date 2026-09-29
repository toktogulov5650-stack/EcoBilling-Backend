using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Controllers.Domain;

namespace EcoBilling.Modules.Controllers.Features.AssignAddress;

public sealed record AssignAddressCommand(
    ControllerId ControllerId,
    AddressId AddressId,
    string ActorId,
    string CorrelationId);
