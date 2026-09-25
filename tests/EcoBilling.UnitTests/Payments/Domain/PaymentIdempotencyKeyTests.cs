using EcoBilling.Modules.Payments.Domain;

namespace EcoBilling.UnitTests.Payments.Domain;

public sealed class PaymentIdempotencyKeyTests
{
    [Fact]
    public void Create_WithOpaqueValue_PreservesValueExactly()
    {
        const string value = " Payment-Request-AbC-123 ";

        var result = PaymentIdempotencyKey.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
        Assert.Equal(value, result.Value.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutValue_ReturnsValidationError(string? value)
    {
        var result = PaymentIdempotencyKey.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.InvalidIdempotencyKey, result.Error);
    }
}
