using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.UnitTests.Tariffs.Domain;

public sealed class TariffTests
{
    [Fact]
    public void Create_WithValidData_CreatesTariffAndNormalizesValues()
    {
        var id = new TariffId(Guid.NewGuid());
        var createdAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(6));

        var result = Tariff.Create(id, "  Население   базовый ", createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal("Население базовый", result.Value.Name.Value);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Fact]
    public void Create_WithInvalidName_ReturnsValidationError()
    {
        var result = Tariff.Create(
            new TariffId(Guid.NewGuid()),
            "   ",
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(TariffErrors.InvalidName, result.Error);
    }
}
