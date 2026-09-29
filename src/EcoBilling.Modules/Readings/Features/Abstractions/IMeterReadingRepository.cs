using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Readings.Domain;

namespace EcoBilling.Modules.Readings.Features.Abstractions;

public interface IMeterReadingRepository
{
    Task<MeterReading?> GetByIdAsync(
        MeterReadingId readingId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MeterReading>> ListByMeterIdAsync(
        MeterId meterId,
        CancellationToken cancellationToken);

    Task<ReadingPersistenceResult> AddAsync(
        MeterReading reading,
        ControllerId? controllerId,
        bool actorIsDirector,
        string correlationId,
        CancellationToken cancellationToken);
}
