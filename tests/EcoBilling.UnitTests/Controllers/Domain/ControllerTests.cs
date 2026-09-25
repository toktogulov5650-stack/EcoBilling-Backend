using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Controllers.Domain;

public sealed class ControllerTests
{
    [Fact]
    public void Create_WithControllerIdentity_CreatesNormalizedProfile()
    {
        var account = CreateUserAccount(UserRole.Controller, LoginType.Email, "agent@example.com");
        var id = new ControllerId(Guid.NewGuid());
        var createdAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(6));

        var result = Controller.Create(id, account, "  Grace Hopper  ", createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal(account.Id, result.Value.UserId);
        Assert.Equal("Grace Hopper", result.Value.FullName);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingFullName_ReturnsValidationError(string? fullName)
    {
        var account = CreateUserAccount(UserRole.Controller, LoginType.Email, "agent@example.com");

        var result = Controller.Create(
            new ControllerId(Guid.NewGuid()),
            account,
            fullName,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ControllerErrors.InvalidFullName, result.Error);
    }

    [Theory]
    [InlineData(UserRole.Resident, LoginType.AccountNumber, "A-100")]
    [InlineData(UserRole.Director, LoginType.Email, "director@example.com")]
    public void Create_WithNonControllerIdentity_ReturnsRoleMismatch(
        UserRole role,
        LoginType loginType,
        string login)
    {
        var account = CreateUserAccount(role, loginType, login);

        var result = Controller.Create(
            new ControllerId(Guid.NewGuid()),
            account,
            "Grace Hopper",
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ControllerErrors.IdentityMustHaveControllerRole, result.Error);
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
