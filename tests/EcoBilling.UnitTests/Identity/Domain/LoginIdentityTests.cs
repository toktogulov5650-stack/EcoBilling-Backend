using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Domain;

public sealed class LoginIdentityTests
{
    [Fact]
    public void Create_RejectsNullValue()
    {
        var result = LoginIdentity.Create(
            UserRole.Resident,
            LoginType.AccountNumber,
            null);

        AssertFailure(result, IdentityErrors.InvalidLogin.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsEmptyValue(string value)
    {
        var result = LoginIdentity.Create(
            UserRole.Resident,
            LoginType.AccountNumber,
            value);

        AssertFailure(result, IdentityErrors.InvalidLogin.Code);
    }

    [Fact]
    public void Create_TrimsAccountNumberWithoutAssumingItsFormat()
    {
        var result = LoginIdentity.Create(
            UserRole.Resident,
            LoginType.AccountNumber,
            "  Ab-12/34  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Ab-12/34", result.Value.NormalizedValue);
    }

    [Fact]
    public void Create_NormalizesEmail()
    {
        var result = LoginIdentity.Create(
            UserRole.Controller,
            LoginType.Email,
            "  Controller@Example.com  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("CONTROLLER@EXAMPLE.COM", result.Value.NormalizedValue);
    }

    [Theory]
    [InlineData("missing-at-sign")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@@example.com")]
    [InlineData("user @example.com")]
    public void Create_RejectsStructurallyInvalidEmail(string email)
    {
        var result = LoginIdentity.Create(
            UserRole.Controller,
            LoginType.Email,
            email);

        AssertFailure(result, IdentityErrors.InvalidLogin.Code);
    }

    [Fact]
    public void Resident_AcceptsOnlyAccountNumber()
    {
        var accepted = LoginIdentity.Create(
            UserRole.Resident,
            LoginType.AccountNumber,
            "A-1");
        var rejected = LoginIdentity.Create(
            UserRole.Resident,
            LoginType.Email,
            "resident@example.com");

        Assert.True(accepted.IsSuccess);
        AssertFailure(rejected, IdentityErrors.InvalidRole.Code);
    }

    [Theory]
    [InlineData(UserRole.Controller)]
    [InlineData(UserRole.Director)]
    public void Staff_AcceptsOnlyEmail(UserRole role)
    {
        var accepted = LoginIdentity.Create(role, LoginType.Email, "staff@example.com");
        var rejected = LoginIdentity.Create(role, LoginType.AccountNumber, "A-1");

        Assert.True(accepted.IsSuccess);
        AssertFailure(rejected, IdentityErrors.InvalidRole.Code);
    }

    [Fact]
    public void Create_RejectsUnsupportedLoginType()
    {
        var result = LoginIdentity.Create(
            UserRole.Resident,
            (LoginType)999,
            "value");

        AssertFailure(result, IdentityErrors.InvalidLogin.Code);
    }

    [Fact]
    public void Create_RejectsUnknownRole()
    {
        var result = LoginIdentity.Create(
            (UserRole)999,
            LoginType.Email,
            "user@example.com");

        AssertFailure(result, IdentityErrors.InvalidRole.Code);
    }

    private static void AssertFailure<T>(
        EcoBilling.SharedKernel.Results.Result<T> result,
        string expectedCode)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }
}
