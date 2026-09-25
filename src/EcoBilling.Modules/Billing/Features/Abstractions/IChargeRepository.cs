using EcoBilling.Modules.Billing.Domain;

namespace EcoBilling.Modules.Billing.Features.Abstractions;

public interface IChargeRepository
{
    Task<Charge?> GetByIdAsync(
        ChargeId chargeId,
        CancellationToken cancellationToken);
}
