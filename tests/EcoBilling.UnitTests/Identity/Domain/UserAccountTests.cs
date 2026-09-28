using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Domain;

public sealed class UserAccountTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 24, 12, 0, 0, TimeSpan.FromHours(6));

    [Fact]
    public void Create_AcceptsValidData()
    {
        var id = new UserId(Guid.NewGuid());
        var login = CreateLogin(LoginType.AccountNumber, "A-100");

        var result = UserAccount.Create(id, login, "stored-hash", UserRole.Resident, CreatedAt);

        Assert.True(result.IsSuccess);
        Assert.Same(id, result.Value.Id);
        Assert.Same(login, result.Value.LoginIdentity);
        Assert.Equal("stored-hash", result.Value.PasswordHash);
        Assert.Equal(UserRole.Resident, result.Value.Role);
        Assert.False(result.Value.RequiresPasswordChange);
        Assert.Equal(CreatedAt.ToUniversalTime(), result.Value.CreatedAt);
        Assert.Equal(0, result.Value.FailedLoginAttempts);
        Assert.Null(result.Value.LockoutEnd);
    }

    [Fact]
    public void Create_WithInitialCredential_MarksPasswordSetupAsRequired()
    {
        var result = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            CreateLogin(LoginType.Email, "director@example.com"),
            "stored-hash",
            UserRole.Director,
            CreatedAt,
            requiresPasswordChange: true);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.RequiresPasswordChange);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsEmptyPasswordHash(string? passwordHash)
    {
        var login = CreateLogin(LoginType.AccountNumber, "A-100");

        var result = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            login,
            passwordHash,
            UserRole.Resident,
            CreatedAt);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidPasswordHash.Code, result.Error.Code);
    }

    [Fact]
    public void Create_RejectsLoginIncompatibleWithRole()
    {
        var residentLogin = CreateLogin(LoginType.AccountNumber, "A-100");

        var result = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            residentLogin,
            "stored-hash",
            UserRole.Controller,
            CreatedAt);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidRole.Code, result.Error.Code);
    }

    [Fact]
    public void Create_RejectsUnknownRole()
    {
        var login = CreateLogin(LoginType.AccountNumber, "A-100");

        var result = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            login,
            "stored-hash",
            (UserRole)999,
            CreatedAt);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidRole.Code, result.Error.Code);
    }

    [Fact]
    public void Model_ContainsNoPlaintextPasswordOrDistrictData()
    {
        var propertyNames = typeof(UserAccount)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(nameof(UserAccount.PasswordHash), propertyNames);
        Assert.DoesNotContain("Password", propertyNames);
        Assert.DoesNotContain("PlaintextPassword", propertyNames);
        Assert.DoesNotContain("DistrictCode", propertyNames);
        Assert.DoesNotContain("DistrictId", propertyNames);
    }

    [Fact]
    public void RecordFailedLogin_LocksAtThresholdAndExpiresAtConfiguredTime()
    {
        var account = CreateAccount();
        var now = CreatedAt.ToUniversalTime();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            account.RecordFailedLogin(now, 5, TimeSpan.FromMinutes(15));
        }

        Assert.True(account.IsLockedOut(now));
        Assert.Equal(5, account.FailedLoginAttempts);
        Assert.Equal(now.AddMinutes(15), account.LockoutEnd);
        Assert.False(account.IsLockedOut(now.AddMinutes(15)));
    }

    [Fact]
    public void RecordFailedLogin_AfterExpiredLockoutStartsNewSequence()
    {
        var account = CreateAccount();
        var now = CreatedAt.ToUniversalTime();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            account.RecordFailedLogin(now, 5, TimeSpan.FromMinutes(15));
        }

        account.RecordFailedLogin(now.AddMinutes(16), 5, TimeSpan.FromMinutes(15));

        Assert.Equal(1, account.FailedLoginAttempts);
        Assert.Null(account.LockoutEnd);
    }

    [Fact]
    public void RecordSuccessfulLogin_ClearsFailureState()
    {
        var account = CreateAccount();
        account.RecordFailedLogin(CreatedAt, 5, TimeSpan.FromMinutes(15));

        account.RecordSuccessfulLogin();

        Assert.Equal(0, account.FailedLoginAttempts);
        Assert.Null(account.LockoutEnd);
    }

    [Fact]
    public void ReplacePasswordHash_RejectsEmptyValue()
    {
        var account = CreateAccount();

        Assert.Throws<ArgumentException>(() => account.ReplacePasswordHash("   "));
    }

    [Fact]
    public void ToString_DoesNotExposePasswordHash()
    {
        var account = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            CreateLogin(LoginType.AccountNumber, "A-100"),
            "sensitive-stored-hash",
            UserRole.Resident,
            CreatedAt).Value;

        Assert.DoesNotContain("sensitive-stored-hash", account.ToString(), StringComparison.Ordinal);
    }

    private static LoginIdentity CreateLogin(LoginType type, string value) =>
        LoginIdentity.Create(type, value).Value;

    private static UserAccount CreateAccount() =>
        UserAccount.Create(
            new UserId(Guid.NewGuid()),
            CreateLogin(LoginType.AccountNumber, "A-100"),
            "stored-hash",
            UserRole.Resident,
            CreatedAt).Value;
}
