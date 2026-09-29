using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class ControllerRepository(EcoBillingDbContext dbContext)
    : IControllerRepository
{
    public Task<Controller?> GetByIdAsync(
        ControllerId controllerId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(controllerId);

        return dbContext.Controllers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                controller => controller.Id == controllerId,
                cancellationToken);
    }

    public Task<Controller?> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        return dbContext.Controllers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                controller => controller.UserId == userId,
                cancellationToken);
    }
}
