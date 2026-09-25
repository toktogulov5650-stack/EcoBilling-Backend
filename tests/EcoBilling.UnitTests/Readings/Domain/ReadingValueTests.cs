using EcoBilling.Modules.Readings.Domain;

namespace EcoBilling.UnitTests.Readings.Domain;

public sealed class ReadingValueTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(12.5)]
    [InlineData(123456.789)]
    public void Create_WithNonNegativeValue_PreservesValue(decimal value)
    {
        var result = ReadingValue.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Fact]
    public void Create_WithNegativeValue_ReturnsValidationError()
    {
        var result = ReadingValue.Create(-0.001m);

        Assert.True(result.IsFailure);
        Assert.Equal(MeterReadingErrors.InvalidValue, result.Error);
    }
}
