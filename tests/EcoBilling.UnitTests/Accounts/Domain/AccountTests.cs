using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.UnitTests.Accounts.Domain;

public sealed class AccountTests
{
    [Fact]
    public void Create_WithValidData_CreatesNormalizedAccount()
    {
        var id = new AccountId(Guid.NewGuid());
        var residentId = new ResidentId(Guid.NewGuid());
        var addressId = new AddressId(Guid.NewGuid());
        var createdAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(6));

        var result = Account.Create(id, residentId, addressId, "  ab-123  ", createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal(residentId, result.Value.ResidentId);
        Assert.Equal(addressId, result.Value.AddressId);
        Assert.Equal("AB-123", result.Value.Number.Value);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingNumber_ReturnsValidationError(string? accountNumber)
    {
        var result = Account.Create(
            new AccountId(Guid.NewGuid()),
            new ResidentId(Guid.NewGuid()),
            new AddressId(Guid.NewGuid()),
            accountNumber,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(AccountErrors.InvalidAccountNumber, result.Error);
    }
}
