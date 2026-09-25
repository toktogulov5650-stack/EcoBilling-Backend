using EcoBilling.Modules.Readings.Contracts;
using EcoBilling.Modules.Readings.Domain;
using EcoBilling.Modules.Readings.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Readings.Features.GetById;

public sealed class GetMeterReadingByIdHandler(
    IMeterReadingRepository meterReadingRepository)
{
    public async Task<Result<MeterReadingDetails>> Handle(
        GetMeterReadingByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.ReadingId);

        var reading = await meterReadingRepository.GetByIdAsync(
            query.ReadingId,
            cancellationToken);

        if (reading is null)
        {
            return Result<MeterReadingDetails>.Failure(MeterReadingErrors.NotFound);
        }

        return Result<MeterReadingDetails>.Success(
            new MeterReadingDetails(
                reading.Id.Value,
                reading.MeterId.Value,
                reading.Value.Value,
                reading.MeasuredAt,
                reading.CreatedAt));
    }
}
