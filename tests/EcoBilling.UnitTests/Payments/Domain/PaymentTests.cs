using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Payments.Domain;

namespace EcoBilling.UnitTests.Payments.Domain;

public sealed class PaymentTests
{
    [Fact]
    public void Create_WithValidData_CreatesPaymentAndNormalizesTimes()
    {
        var id = new PaymentId(Guid.NewGuid());
        var accountId = new AccountId(Guid.NewGuid());
        var paidAt = new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.FromHours(6));
        var createdAt = new DateTimeOffset(2026, 9, 25, 10, 1, 0, TimeSpan.FromHours(6));

        var result = Payment.Create(
            id,
            accountId,
            123.456m,
            "Payment-Request-AbC-123",
            paidAt,
            createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal(accountId, result.Value.AccountId);
        Assert.Equal(123.456m, result.Value.Amount.Value);
        Assert.Equal("Payment-Request-AbC-123", result.Value.IdempotencyKey.Value);
        Assert.Equal(paidAt.ToUniversalTime(), result.Value.PaidAt);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Fact]
    public void Create_WithInvalidAmount_ReturnsValidationError()
    {
        var result = Payment.Create(
            new PaymentId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            0m,
            "payment-request-123",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.InvalidAmount, result.Error);
    }

    [Fact]
    public void Create_WithInvalidIdempotencyKey_ReturnsValidationError()
    {
        var result = Payment.Create(
            new PaymentId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            100m,
            "   ",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.InvalidIdempotencyKey, result.Error);
    }
}
