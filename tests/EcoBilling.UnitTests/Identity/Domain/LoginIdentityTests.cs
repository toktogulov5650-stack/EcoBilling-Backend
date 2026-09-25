using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Domain;

public sealed class LoginIdentityTests
{
    [Fact]
    public void Create_RejectsNullValue()
    {
        var result = LoginIdentity.Create(
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
            LoginType.AccountNumber,
            value);

        AssertFailure(result, IdentityErrors.InvalidLogin.Code);
    }

    [Fact]
    public void Create_NormalizesAccountNumberWithoutAssumingItsFormat()
    {
        var result = LoginIdentity.Create(
            LoginType.AccountNumber,
            "  Ab-12/34  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("AB-12/34", result.Value.NormalizedValue);
    }

    [Fact]
    public void Create_NormalizesEmail()
    {
        var result = LoginIdentity.Create(
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
            LoginType.Email,
            email);

        AssertFailure(result, IdentityErrors.InvalidLogin.Code);
    }

    [Fact]
    public void Resident_AcceptsOnlyAccountNumber()
    {
        var accountNumber = LoginIdentity.Create(LoginType.AccountNumber, "A-1").Value;
        var email = LoginIdentity.Create(LoginType.Email, "resident@example.com").Value;

        Assert.True(accountNumber.IsCompatibleWith(UserRole.Resident));
        Assert.False(email.IsCompatibleWith(UserRole.Resident));
    }

    [Theory]
    [InlineData(UserRole.Controller)]
    [InlineData(UserRole.Director)]
    public void Staff_AcceptsOnlyEmail(UserRole role)
    {
        var email = LoginIdentity.Create(LoginType.Email, "staff@example.com").Value;
        var accountNumber = LoginIdentity.Create(LoginType.AccountNumber, "A-1").Value;

        Assert.True(email.IsCompatibleWith(role));
        Assert.False(accountNumber.IsCompatibleWith(role));
    }

    [Fact]
    public void Create_RejectsUnsupportedLoginType()
    {
        var result = LoginIdentity.Create(
            (LoginType)999,
            "value");

        AssertFailure(result, IdentityErrors.InvalidLogin.Code);
    }

    private static void AssertFailure<T>(
        EcoBilling.SharedKernel.Results.Result<T> result,
        string expectedCode)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }
}
