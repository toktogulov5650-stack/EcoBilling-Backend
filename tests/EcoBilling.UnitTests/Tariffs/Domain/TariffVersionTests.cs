using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.UnitTests.Tariffs.Domain;

public sealed class TariffVersionTests
{
    [Fact]
    public void Create_WithValidData_CreatesVersionAndNormalizesCreatedAt()
    {
        var id = new TariffVersionId(Guid.NewGuid());
        var tariffId = new TariffId(Guid.NewGuid());
        var effectiveFrom = new DateOnly(2026, 1, 1);
        var effectiveTo = new DateOnly(2026, 7, 1);
        var createdAt = new DateTimeOffset(2025, 12, 20, 10, 0, 0, TimeSpan.FromHours(6));

        var result = TariffVersion.Create(
            id,
            tariffId,
            12.3456789m,
            effectiveFrom,
            effectiveTo,
            createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal(tariffId, result.Value.TariffId);
        Assert.Equal(12.3456789m, result.Value.Rate.Value);
        Assert.Equal(effectiveFrom, result.Value.EffectiveFrom);
        Assert.Equal(effectiveTo, result.Value.EffectiveTo);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Fact]
    public void Create_WithOpenEndedPeriod_CreatesVersion()
    {
        var result = TariffVersion.Create(
            new TariffVersionId(Guid.NewGuid()),
            new TariffId(Guid.NewGuid()),
            12m,
            new DateOnly(2026, 1, 1),
            null,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.EffectiveTo);
    }

    [Fact]
    public void Create_WithNegativeRate_ReturnsValidationError()
    {
        var result = TariffVersion.Create(
            new TariffVersionId(Guid.NewGuid()),
            new TariffId(Guid.NewGuid()),
            -1m,
            new DateOnly(2026, 1, 1),
            null,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(TariffErrors.InvalidRate, result.Error);
    }

    [Theory]
    [InlineData(2026, 1, 1)]
    [InlineData(2025, 12, 31)]
    public void Create_WithEndNotAfterStart_ReturnsValidationError(
        int year,
        int month,
        int day)
    {
        var result = TariffVersion.Create(
            new TariffVersionId(Guid.NewGuid()),
            new TariffId(Guid.NewGuid()),
            12m,
            new DateOnly(2026, 1, 1),
            new DateOnly(year, month, day),
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(TariffErrors.InvalidEffectivePeriod, result.Error);
    }
}
