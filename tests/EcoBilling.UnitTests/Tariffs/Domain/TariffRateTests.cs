using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.UnitTests.Tariffs.Domain;

public sealed class TariffRateTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1.25)]
    [InlineData(123456.789)]
    public void Create_WithNonNegativeRate_PreservesValue(decimal value)
    {
        var result = TariffRate.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Fact]
    public void Create_WithNegativeRate_ReturnsValidationError()
    {
        var result = TariffRate.Create(-0.001m);

        Assert.True(result.IsFailure);
        Assert.Equal(TariffErrors.InvalidRate, result.Error);
    }
}
