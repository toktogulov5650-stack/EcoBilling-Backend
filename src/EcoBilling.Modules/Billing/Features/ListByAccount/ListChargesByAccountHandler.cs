using EcoBilling.Modules.Billing.Contracts;
using EcoBilling.Modules.Billing.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Billing.Features.ListByAccount;

public sealed class ListChargesByAccountHandler(
    IBillingCalculationRepository repository)
{
    public async Task<Result<IReadOnlyList<ChargeDetails>>> Handle(
        ListChargesByAccountQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.AccountId);

        var charges = await repository.ListByAccountAsync(
            query.AccountId,
            cancellationToken);

        return Result<IReadOnlyList<ChargeDetails>>.Success(
            charges.Select(
                charge => new ChargeDetails(
                    charge.Id.Value,
                    charge.AccountId.Value,
                    charge.TariffVersionId.Value,
                    charge.PeriodStart,
                    charge.PeriodEnd,
                    charge.Amount,
                    charge.CreatedAt,
                    charge.PreviousReadingId?.Value,
                    charge.CurrentReadingId?.Value,
                    charge.Consumption,
                    charge.CalculationVersion,
                    charge.Currency))
                .ToArray());
    }
}
