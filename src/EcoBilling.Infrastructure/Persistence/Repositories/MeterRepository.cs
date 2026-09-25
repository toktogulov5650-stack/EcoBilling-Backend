using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Meters.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class MeterRepository(EcoBillingDbContext dbContext)
    : IMeterRepository
{
    public Task<Meter?> GetByIdAsync(
        MeterId meterId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(meterId);

        return dbContext.Meters
            .AsNoTracking()
            .SingleOrDefaultAsync(
                meter => meter.Id == meterId,
                cancellationToken);
    }
}
