using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Domain;

public sealed class DirectorProfileTests
{
    [Fact]
    public void Create_WithDirectorIdentity_CreatesNormalizedProfile()
    {
        var account = CreateUserAccount(UserRole.Director, LoginType.Email, "director@example.com");
        var id = new DirectorId(Guid.NewGuid());
        var createdAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(6));

        var result = DirectorProfile.Create(id, account, "  Ada Lovelace  ", createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal(account.Id, result.Value.UserId);
        Assert.Equal("Ada Lovelace", result.Value.FullName);
        Assert.Equal(DirectorProfile.InstanceSlot, result.Value.Slot);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutFullName_ReturnsValidationError(string? fullName)
    {
        var account = CreateUserAccount(UserRole.Director, LoginType.Email, "director@example.com");

        var result = DirectorProfile.Create(
            new DirectorId(Guid.NewGuid()),
            account,
            fullName,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(DirectorProvisioningErrors.InvalidFullName, result.Error);
    }

    [Theory]
    [InlineData(UserRole.Resident, LoginType.AccountNumber, "A-100")]
    [InlineData(UserRole.Controller, LoginType.Email, "controller@example.com")]
    public void Create_WithNonDirectorIdentity_ReturnsRoleMismatch(
        UserRole role,
        LoginType loginType,
        string login)
    {
        var account = CreateUserAccount(role, loginType, login);

        var result = DirectorProfile.Create(
            new DirectorId(Guid.NewGuid()),
            account,
            "Ada Lovelace",
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(DirectorProvisioningErrors.IdentityMustHaveDirectorRole, result.Error);
    }

    private static UserAccount CreateUserAccount(
        UserRole role,
        LoginType loginType,
        string login) =>
        UserAccount.Create(
            new UserId(Guid.NewGuid()),
            LoginIdentity.Create(loginType, login).Value,
            "stored-password-hash",
            role,
            DateTimeOffset.UtcNow).Value;
}
