using EcoBilling.Modules.Payments.Domain;
using EcoBilling.Modules.Payments.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(EcoBillingDbContext dbContext)
    : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(paymentId);

        return dbContext.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                payment => payment.Id == paymentId,
                cancellationToken);
    }
}
