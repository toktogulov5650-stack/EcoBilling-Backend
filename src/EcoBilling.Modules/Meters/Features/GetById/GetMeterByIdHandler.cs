using EcoBilling.Modules.Meters.Contracts;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Meters.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Meters.Features.GetById;

public sealed class GetMeterByIdHandler(IMeterRepository meterRepository)
{
    public async Task<Result<MeterDetails>> Handle(
        GetMeterByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.MeterId);

        var meter = await meterRepository.GetByIdAsync(
            query.MeterId,
            cancellationToken);

        if (meter is null)
        {
            return Result<MeterDetails>.Failure(MeterErrors.NotFound);
        }

        return Result<MeterDetails>.Success(
            new MeterDetails(
                meter.Id.Value,
                meter.AccountId.Value,
                meter.SerialNumber.Value,
                meter.InstalledAt,
                meter.CreatedAt));
    }
}
