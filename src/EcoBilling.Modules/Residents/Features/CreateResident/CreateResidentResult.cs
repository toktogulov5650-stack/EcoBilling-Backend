using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.Modules.Residents.Features.CreateResident;

public sealed record CreateResidentResult(
    ResidentId ResidentId,
    AccountId AccountId,
    AddressId AddressId,
    ResidentCreationOperationId OperationId,
    bool IsReplay);
