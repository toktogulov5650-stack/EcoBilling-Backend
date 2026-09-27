using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Identity.Application.Abstractions;

public interface IDirectorProvisioningRepository
{
    Task<DirectorProvisioningPersistenceResult> ProvisionAsync(
        UserAccount userAccount,
        DirectorProfile director,
        DirectorProvisioningOperation operation,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken);
}
