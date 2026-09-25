using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class TariffVersionRepository(EcoBillingDbContext dbContext)
    : ITariffVersionRepository
{
    public Task<TariffVersion?> GetByIdAsync(
        TariffVersionId tariffVersionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tariffVersionId);

        return dbContext.TariffVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                version => version.Id == tariffVersionId,
                cancellationToken);
    }
}
