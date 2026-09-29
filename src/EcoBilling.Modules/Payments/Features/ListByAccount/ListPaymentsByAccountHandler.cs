using EcoBilling.Modules.Payments.Contracts;
using EcoBilling.Modules.Payments.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Payments.Features.ListByAccount;

public sealed class ListPaymentsByAccountHandler(IPaymentRepository repository)
{
    public async Task<Result<IReadOnlyList<PaymentDetails>>> Handle(
        ListPaymentsByAccountQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.AccountId);

        var payments = await repository.ListByAccountAsync(
            query.AccountId,
            cancellationToken);

        return Result<IReadOnlyList<PaymentDetails>>.Success(
            payments.Select(
                payment => new PaymentDetails(
                    payment.Id.Value,
                    payment.AccountId.Value,
                    payment.Amount.Value,
                    payment.IdempotencyKey.Value,
                    payment.PaidAt,
                    payment.CreatedAt))
                .ToArray());
    }
}
