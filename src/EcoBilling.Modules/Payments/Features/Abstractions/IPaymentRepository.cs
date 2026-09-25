using EcoBilling.Modules.Payments.Domain;

namespace EcoBilling.Modules.Payments.Features.Abstractions;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken);
}
