using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Billing.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class ChargeRepository(EcoBillingDbContext dbContext)
    : IChargeRepository
{
    public Task<Charge?> GetByIdAsync(
        ChargeId chargeId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chargeId);

        return dbContext.Charges
            .AsNoTracking()
            .SingleOrDefaultAsync(
                charge => charge.Id == chargeId,
                cancellationToken);
    }
}
