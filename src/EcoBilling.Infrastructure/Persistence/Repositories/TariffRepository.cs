using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class TariffRepository(EcoBillingDbContext dbContext)
    : ITariffRepository
{
    public Task<Tariff?> GetByIdAsync(
        TariffId tariffId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tariffId);

        return dbContext.Tariffs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                tariff => tariff.Id == tariffId,
                cancellationToken);
    }
}
