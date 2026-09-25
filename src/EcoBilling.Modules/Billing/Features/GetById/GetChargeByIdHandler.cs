using EcoBilling.Modules.Billing.Contracts;
using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Billing.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Billing.Features.GetById;

public sealed class GetChargeByIdHandler(IChargeRepository chargeRepository)
{
    public async Task<Result<ChargeDetails>> Handle(
        GetChargeByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.ChargeId);

        var charge = await chargeRepository.GetByIdAsync(
            query.ChargeId,
            cancellationToken);

        if (charge is null)
        {
            return Result<ChargeDetails>.Failure(ChargeErrors.NotFound);
        }

        return Result<ChargeDetails>.Success(
            new ChargeDetails(
                charge.Id.Value,
                charge.AccountId.Value,
                charge.TariffVersionId.Value,
                charge.PeriodStart,
                charge.PeriodEnd,
                charge.Amount,
                charge.CreatedAt));
    }
}
