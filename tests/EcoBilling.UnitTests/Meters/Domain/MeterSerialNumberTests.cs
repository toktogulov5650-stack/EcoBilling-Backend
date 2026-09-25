using EcoBilling.Modules.Meters.Domain;

namespace EcoBilling.UnitTests.Meters.Domain;

public sealed class MeterSerialNumberTests
{
    [Fact]
    public void Create_WithValue_NormalizesUsingTrimAndUpperInvariant()
    {
        var result = MeterSerialNumber.Create("  sn-ab-123  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("SN-AB-123", result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingValue_ReturnsValidationError(string? value)
    {
        var result = MeterSerialNumber.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(MeterErrors.InvalidSerialNumber, result.Error);
    }

    [Fact]
    public void Create_WithEquivalentValues_ProducesEqualSerialNumbers()
    {
        var first = MeterSerialNumber.Create("sn-ab-123").Value;
        var second = MeterSerialNumber.Create(" SN-AB-123 ").Value;

        Assert.Equal(first, second);
    }
}
