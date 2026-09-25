using EcoBilling.Modules.Payments.Contracts;
using EcoBilling.Modules.Payments.Domain;
using EcoBilling.Modules.Payments.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Payments.Features.GetById;

public sealed class GetPaymentByIdHandler(IPaymentRepository paymentRepository)
{
    public async Task<Result<PaymentDetails>> Handle(
        GetPaymentByIdQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.PaymentId);

        var payment = await paymentRepository.GetByIdAsync(
            query.PaymentId,
            cancellationToken);

        if (payment is null)
        {
            return Result<PaymentDetails>.Failure(PaymentErrors.NotFound);
        }

        return Result<PaymentDetails>.Success(
            new PaymentDetails(
                payment.Id.Value,
                payment.AccountId.Value,
                payment.Amount.Value,
                payment.IdempotencyKey.Value,
                payment.PaidAt,
                payment.CreatedAt));
    }
}
