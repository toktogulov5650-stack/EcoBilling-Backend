using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Readings.Domain;

namespace EcoBilling.UnitTests.Readings.Domain;

public sealed class MeterReadingTests
{
    [Fact]
    public void Create_WithValidData_CreatesReadingAndNormalizesTimes()
    {
        var id = new MeterReadingId(Guid.NewGuid());
        var meterId = new MeterId(Guid.NewGuid());
        var measuredAt = new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(6));
        var createdAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(6));

        var result = MeterReading.Create(
            id,
            meterId,
            123.456m,
            measuredAt,
            createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal(meterId, result.Value.MeterId);
        Assert.Equal(123.456m, result.Value.Value.Value);
        Assert.Equal(measuredAt.ToUniversalTime(), result.Value.MeasuredAt);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Fact]
    public void Create_WithNegativeValue_ReturnsValidationError()
    {
        var result = MeterReading.Create(
            new MeterReadingId(Guid.NewGuid()),
            new MeterId(Guid.NewGuid()),
            -1m,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(MeterReadingErrors.InvalidValue, result.Error);
    }
}
