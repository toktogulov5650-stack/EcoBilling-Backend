using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.UnitTests.Billing.Domain;

public sealed class ChargeTests
{
    [Theory]
    [InlineData(-10.5)]
    [InlineData(0)]
    [InlineData(123.456)]
    public void Create_WithConfirmedStructuralData_PreservesAmount(decimal amount)
    {
        var id = new ChargeId(Guid.NewGuid());
        var accountId = new AccountId(Guid.NewGuid());
        var tariffVersionId = new TariffVersionId(Guid.NewGuid());
        var periodStart = new DateOnly(2026, 1, 1);
        var periodEnd = new DateOnly(2026, 2, 1);
        var createdAt = new DateTimeOffset(2026, 2, 2, 10, 0, 0, TimeSpan.FromHours(6));

        var result = Charge.Create(
            id,
            accountId,
            tariffVersionId,
            periodStart,
            periodEnd,
            amount,
            createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal(accountId, result.Value.AccountId);
        Assert.Equal(tariffVersionId, result.Value.TariffVersionId);
        Assert.Equal(periodStart, result.Value.PeriodStart);
        Assert.Equal(periodEnd, result.Value.PeriodEnd);
        Assert.Equal(amount, result.Value.Amount);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Fact]
    public void Create_WithInvalidPeriod_ReturnsValidationError()
    {
        var result = Charge.Create(
            new ChargeId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            new TariffVersionId(Guid.NewGuid()),
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 1, 1),
            100m,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ChargeErrors.InvalidBillingPeriod, result.Error);
    }
}
