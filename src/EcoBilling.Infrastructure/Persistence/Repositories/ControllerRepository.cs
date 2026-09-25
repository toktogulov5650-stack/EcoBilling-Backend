using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class ControllerRepository(EcoBillingDbContext dbContext)
    : IControllerRepository
{
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
