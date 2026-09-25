using EcoBilling.Modules.Readings.Domain;
using EcoBilling.Modules.Readings.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class MeterReadingRepository(EcoBillingDbContext dbContext)
    : IMeterReadingRepository
{
    public Task<MeterReading?> GetByIdAsync(
        MeterReadingId readingId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(readingId);

        return dbContext.MeterReadings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                reading => reading.Id == readingId,
                cancellationToken);
    }
}
