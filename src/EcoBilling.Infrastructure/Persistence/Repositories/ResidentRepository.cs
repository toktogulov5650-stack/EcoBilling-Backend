using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class ResidentRepository(EcoBillingDbContext dbContext)
    : IResidentRepository
{
    public Task<Resident?> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        return dbContext.Residents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                resident => resident.UserId == userId,
                cancellationToken);
    }
}
