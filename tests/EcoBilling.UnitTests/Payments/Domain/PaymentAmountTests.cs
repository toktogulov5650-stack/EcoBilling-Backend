using EcoBilling.Modules.Payments.Domain;

namespace EcoBilling.UnitTests.Payments.Domain;

public sealed class PaymentAmountTests
{
    [Theory]
    [InlineData(0.001)]
    [InlineData(12.5)]
    [InlineData(123456.789)]
    public void Create_WithPositiveAmount_PreservesValue(decimal value)
    {
        var result = PaymentAmount.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.001)]
    public void Create_WithNonPositiveAmount_ReturnsValidationError(decimal value)
    {
        var result = PaymentAmount.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.InvalidAmount, result.Error);
    }
}
