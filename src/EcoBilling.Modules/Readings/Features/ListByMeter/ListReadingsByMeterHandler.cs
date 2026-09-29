using EcoBilling.Modules.Readings.Contracts;
using EcoBilling.Modules.Readings.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Readings.Features.ListByMeter;

public sealed class ListReadingsByMeterHandler(IMeterReadingRepository repository)
{
    public async Task<Result<IReadOnlyList<MeterReadingDetails>>> Handle(
        ListReadingsByMeterQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.MeterId);

        var readings = await repository.ListByMeterIdAsync(
            query.MeterId,
            cancellationToken);

        return Result<IReadOnlyList<MeterReadingDetails>>.Success(
            readings.Select(
                reading => new MeterReadingDetails(
                    reading.Id.Value,
                    reading.MeterId.Value,
                    reading.Value.Value,
                    reading.MeasuredAt,
                    reading.CreatedAt,
                    reading.AuthorUserId?.Value,
                    reading.Source.ToString(),
                    reading.SupersedesReadingId?.Value,
                    reading.CorrectionReason))
                .ToArray());
    }
}
