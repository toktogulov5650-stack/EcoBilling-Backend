using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Controllers.Features.Abstractions;

public interface IControllerCreationRepository
{
    Task<ControllerCreationPersistenceResult> CreateAsync(
        UserAccount userAccount,
        Controller controller,
        ControllerCreationOperation operation,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken);
}
