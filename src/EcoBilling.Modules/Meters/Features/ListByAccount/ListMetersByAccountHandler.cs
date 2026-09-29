using EcoBilling.Modules.Meters.Contracts;
using EcoBilling.Modules.Meters.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Meters.Features.ListByAccount;

public sealed class ListMetersByAccountHandler(IMeterRepository repository)
{
    public async Task<Result<IReadOnlyList<MeterDetails>>> Handle(
        ListMetersByAccountQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.AccountId);

        var meters = await repository.ListByAccountIdAsync(
            query.AccountId,
            cancellationToken);

        return Result<IReadOnlyList<MeterDetails>>.Success(
            meters.Select(
                meter => new MeterDetails(
                    meter.Id.Value,
                    meter.AccountId.Value,
                    meter.SerialNumber.Value,
                    meter.InstalledAt,
                    meter.CreatedAt,
                    meter.IsActive,
                    meter.RetiredAt,
                    meter.ReplacesMeterId?.Value))
                .ToArray());
    }
}
