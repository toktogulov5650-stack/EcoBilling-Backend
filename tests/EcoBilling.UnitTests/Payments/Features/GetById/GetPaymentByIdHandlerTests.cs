using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Payments.Domain;
using EcoBilling.Modules.Payments.Features.Abstractions;
using EcoBilling.Modules.Payments.Features.GetById;

namespace EcoBilling.UnitTests.Payments.Features.GetById;

public sealed class GetPaymentByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenPaymentExists_ReturnsDetails()
    {
        var payment = CreatePayment();
        var repository = new RecordingPaymentRepository(payment);
        var handler = new GetPaymentByIdHandler(repository);

        var result = await handler.Handle(
            new GetPaymentByIdQuery(payment.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(payment.Id.Value, result.Value.Id);
        Assert.Equal(payment.AccountId.Value, result.Value.AccountId);
        Assert.Equal(payment.Amount.Value, result.Value.Amount);
        Assert.Equal(payment.IdempotencyKey.Value, result.Value.IdempotencyKey);
        Assert.Equal(payment.PaidAt, result.Value.PaidAt);
        Assert.Equal(payment.CreatedAt, result.Value.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenPaymentDoesNotExist_ReturnsNotFound()
    {
        var handler = new GetPaymentByIdHandler(
            new RecordingPaymentRepository(null));

        var result = await handler.Handle(
            new GetPaymentByIdQuery(new PaymentId(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_ForwardsPaymentIdAndCancellationToken()
    {
        var paymentId = new PaymentId(Guid.NewGuid());
        using var cancellationTokenSource = new CancellationTokenSource();
        var repository = new RecordingPaymentRepository(null);
        var handler = new GetPaymentByIdHandler(repository);

        await handler.Handle(
            new GetPaymentByIdQuery(paymentId),
            cancellationTokenSource.Token);

        Assert.Equal(paymentId, repository.ReceivedPaymentId);
        Assert.Equal(cancellationTokenSource.Token, repository.ReceivedCancellationToken);
    }

    private static Payment CreatePayment() =>
        Payment.Create(
            new PaymentId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            123.456m,
            "payment-request-123",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow).Value;

    private sealed class RecordingPaymentRepository(Payment? payment)
        : IPaymentRepository
    {
        public PaymentId? ReceivedPaymentId { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<Payment?> GetByIdAsync(
            PaymentId paymentId,
            CancellationToken cancellationToken)
        {
            ReceivedPaymentId = paymentId;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(payment);
        }
    }
}
