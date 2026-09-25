using EcoBilling.Modules.Billing.Domain;

namespace EcoBilling.UnitTests.Billing.Domain;

public sealed class BillingPeriodTests
{
    [Fact]
    public void Create_WithEndAfterStart_CreatesHalfOpenPeriod()
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 2, 1);

        var result = BillingPeriod.Create(start, end);

        Assert.True(result.IsSuccess);
        Assert.Equal(start, result.Value.Start);
        Assert.Equal(end, result.Value.End);
        Assert.Equal("[2026-01-01, 2026-02-01)", result.Value.ToString());
    }

    [Theory]
    [InlineData(2026, 1, 1)]
    [InlineData(2025, 12, 31)]
    public void Create_WithEndNotAfterStart_ReturnsValidationError(
        int year,
        int month,
        int day)
    {
        var result = BillingPeriod.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(year, month, day));

        Assert.True(result.IsFailure);
        Assert.Equal(ChargeErrors.InvalidBillingPeriod, result.Error);
    }
}
