using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Meters.Domain;

namespace EcoBilling.UnitTests.Meters.Domain;

public sealed class MeterTests
{
    [Fact]
    public void Create_WithValidData_CreatesNormalizedMeter()
    {
        var id = new MeterId(Guid.NewGuid());
        var accountId = new AccountId(Guid.NewGuid());
        var installedAt = new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(6));
        var createdAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(6));

        var result = Meter.Create(
            id,
            accountId,
            "  sn-ab-123  ",
            installedAt,
            createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal(accountId, result.Value.AccountId);
        Assert.Equal("SN-AB-123", result.Value.SerialNumber.Value);
        Assert.Equal(installedAt.ToUniversalTime(), result.Value.InstalledAt);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingSerialNumber_ReturnsValidationError(string? serialNumber)
    {
        var result = Meter.Create(
            new MeterId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            serialNumber,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(MeterErrors.InvalidSerialNumber, result.Error);
    }
}
