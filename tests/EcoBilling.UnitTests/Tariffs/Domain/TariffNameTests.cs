using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.UnitTests.Tariffs.Domain;

public sealed class TariffNameTests
{
    [Fact]
    public void Create_WithText_NormalizesWhitespace()
    {
        var result = TariffName.Create("  Население\t базовый  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Население базовый", result.Value.Value);
        Assert.Equal("Население базовый", result.Value.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutText_ReturnsValidationError(string? value)
    {
        var result = TariffName.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(TariffErrors.InvalidName, result.Error);
    }
}
