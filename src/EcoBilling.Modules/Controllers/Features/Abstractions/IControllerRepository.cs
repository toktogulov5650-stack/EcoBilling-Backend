using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.Modules.Controllers.Features.Abstractions;

public interface IControllerRepository
{
    Task<Controller?> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
