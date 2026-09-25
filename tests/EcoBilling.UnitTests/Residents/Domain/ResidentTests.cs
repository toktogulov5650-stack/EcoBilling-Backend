using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.UnitTests.Residents.Domain;

public sealed class ResidentTests
{
    [Fact]
    public void Create_WithResidentIdentity_CreatesNormalizedProfile()
    {
        var account = CreateUserAccount(UserRole.Resident, LoginType.AccountNumber, "A-100");
        var id = new ResidentId(Guid.NewGuid());
        var createdAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(6));

        var result = Resident.Create(id, account, "  Ada Lovelace  ", createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal(account.Id, result.Value.UserId);
        Assert.Equal("Ada Lovelace", result.Value.FullName);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingFullName_ReturnsValidationError(string? fullName)
    {
        var account = CreateUserAccount(UserRole.Resident, LoginType.AccountNumber, "A-100");

        var result = Resident.Create(
            new ResidentId(Guid.NewGuid()),
            account,
            fullName,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ResidentErrors.InvalidFullName, result.Error);
    }

    [Theory]
    [InlineData(UserRole.Controller)]
    [InlineData(UserRole.Director)]
    public void Create_WithStaffIdentity_ReturnsRoleMismatch(UserRole role)
    {
        var account = CreateUserAccount(role, LoginType.Email, $"{role}@example.com");

        var result = Resident.Create(
            new ResidentId(Guid.NewGuid()),
            account,
            "Ada Lovelace",
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ResidentErrors.IdentityMustHaveResidentRole, result.Error);
    }

    private static UserAccount CreateUserAccount(
        UserRole role,
        LoginType loginType,
        string login)
    {
        var loginIdentity = LoginIdentity.Create(loginType, login).Value;
        return UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity,
            "stored-password-hash",
            role,
            DateTimeOffset.UtcNow).Value;
    }
}
