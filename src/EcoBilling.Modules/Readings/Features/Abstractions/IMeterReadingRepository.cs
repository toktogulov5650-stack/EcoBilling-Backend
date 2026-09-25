using EcoBilling.Modules.Readings.Domain;

namespace EcoBilling.Modules.Readings.Features.Abstractions;

public interface IMeterReadingRepository
{
    Task<MeterReading?> GetByIdAsync(
        MeterReadingId readingId,
        CancellationToken cancellationToken);
}
