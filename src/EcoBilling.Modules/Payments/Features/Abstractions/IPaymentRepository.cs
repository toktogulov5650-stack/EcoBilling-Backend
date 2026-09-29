using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Payments.Contracts;
using EcoBilling.Modules.Payments.Domain;

namespace EcoBilling.Modules.Payments.Features.Abstractions;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(
        PaymentId paymentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Payment>> ListByAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken);

    Task<AccountFinancialSummary?> GetFinancialSummaryAsync(
        AccountId accountId,
        CancellationToken cancellationToken);

    Task<PaymentRegistrationPersistenceResult> RegisterAsync(
        Payment payment,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken);
}
