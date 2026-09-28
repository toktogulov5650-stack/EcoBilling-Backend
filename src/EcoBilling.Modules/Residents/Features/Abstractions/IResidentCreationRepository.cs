using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.Modules.Residents.Features.Abstractions;

public interface IResidentCreationRepository
{
    Task<ResidentCreationPersistenceResult> CreateAsync(
        UserAccount userAccount,
        Resident resident,
        Address address,
        Account account,
        ResidentCreationOperation operation,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken);
}
