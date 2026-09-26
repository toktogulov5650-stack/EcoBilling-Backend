using EcoBilling.Modules.Identity.Application;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.Authenticate;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Application;

public sealed class AuthenticateHandlerTests
{
    private const string ValidPassword = "test-password";
    private const string StoredHash = "test-password-hash";
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 24, 12, 0, 0, TimeSpan.FromHours(6));

    [Theory]
    [InlineData(UserRole.Resident, LoginType.AccountNumber, "A-100")]
    [InlineData(UserRole.Controller, LoginType.Email, "controller@example.com")]
    [InlineData(UserRole.Director, LoginType.Email, "director@example.com")]
    public async Task Handle_ReturnsUserIdAndRoleForValidCredentials(
        UserRole role,
        LoginType loginType,
        string login)
    {
        var account = CreateAccount(role, loginType, login);
        var repository = new RecordingUserAccountRepository(account);
        var handler = CreateHandler(repository, passwordMatches: true);

        var result = await handler.Handle(
            new AuthenticateCommand(loginType, login, ValidPassword),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(account.Id, result.Value.User.UserId);
        Assert.Equal(role, result.Value.User.Role);
    }

    [Fact]
    public async Task Handle_ReturnsInvalidCredentialsWhenUserDoesNotExist()
    {
        var handler = CreateHandler(new RecordingUserAccountRepository(null), passwordMatches: true);

        var result = await handler.Handle(
            new AuthenticateCommand(LoginType.Email, "missing@example.com", ValidPassword),
            CancellationToken.None);

        AssertInvalidCredentials(result);
    }

    [Fact]
    public async Task Handle_ReturnsInvalidCredentialsWhenPasswordIsWrong()
    {
        var account = CreateAccount(UserRole.Controller, LoginType.Email, "user@example.com");
        var handler = CreateHandler(
            new RecordingUserAccountRepository(account),
            passwordMatches: false);

        var result = await handler.Handle(
            new AuthenticateCommand(LoginType.Email, "user@example.com", "wrong-password"),
            CancellationToken.None);

        AssertInvalidCredentials(result);
    }

    [Fact]
    public async Task Handle_AcceptsPasswordThatNeedsRehashWithoutChangingStoredHash()
    {
        var account = CreateAccount(UserRole.Controller, LoginType.Email, "user@example.com");
        var repository = new RecordingUserAccountRepository(account);
        var handler = new AuthenticateHandler(
            repository,
            new ConfigurablePasswordHasher(PasswordVerificationOutcome.SuccessRehashNeeded),
            new LoginNormalizer());

        var result = await handler.Handle(
            new AuthenticateCommand(LoginType.Email, "user@example.com", ValidPassword),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(StoredHash, account.PasswordHash);
    }

    [Fact]
    public async Task Handle_RejectsInitialCredentialUntilPasswordSetupCompletes()
    {
        var loginIdentity = LoginIdentity.Create(
            LoginType.Email,
            "director@example.com").Value;
        var account = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity,
            StoredHash,
            UserRole.Director,
            CreatedAt,
            requiresPasswordChange: true).Value;
        var handler = CreateHandler(
            new RecordingUserAccountRepository(account),
            passwordMatches: true);

        var result = await handler.Handle(
            new AuthenticateCommand(
                LoginType.Email,
                "director@example.com",
                ValidPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.PasswordSetupRequired, result.Error);
    }

    [Fact]
    public async Task Handle_HidesWhetherAccountExists()
    {
        var missingUserHandler = CreateHandler(
            new RecordingUserAccountRepository(null),
            passwordMatches: true);
        var existingAccount = CreateAccount(
            UserRole.Controller,
            LoginType.Email,
            "user@example.com");
        var wrongPasswordHandler = CreateHandler(
            new RecordingUserAccountRepository(existingAccount),
            passwordMatches: false);
        var command = new AuthenticateCommand(
            LoginType.Email,
            "user@example.com",
            "wrong-password");

        var missingUserResult = await missingUserHandler.Handle(command, CancellationToken.None);
        var wrongPasswordResult = await wrongPasswordHandler.Handle(command, CancellationToken.None);

        Assert.Equal(missingUserResult.Error, wrongPasswordResult.Error);
        Assert.Equal("auth.invalid_credentials", missingUserResult.Error.Code);
    }

    [Fact]
    public async Task Handle_PassesNormalizedLoginToRepository()
    {
        var account = CreateAccount(UserRole.Controller, LoginType.Email, "user@example.com");
        var repository = new RecordingUserAccountRepository(account);
        var handler = CreateHandler(repository, passwordMatches: true);

        await handler.Handle(
            new AuthenticateCommand(LoginType.Email, "  User@Example.com  ", ValidPassword),
            CancellationToken.None);

        Assert.Equal(LoginType.Email, repository.ReceivedLoginIdentity?.Type);
        Assert.Equal("USER@EXAMPLE.COM", repository.ReceivedLoginIdentity?.NormalizedValue);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToRepository()
    {
        var account = CreateAccount(UserRole.Resident, LoginType.AccountNumber, "A-100");
        var repository = new RecordingUserAccountRepository(account);
        var handler = CreateHandler(repository, passwordMatches: true);
        using var cancellationTokenSource = new CancellationTokenSource();

        await handler.Handle(
            new AuthenticateCommand(LoginType.AccountNumber, "A-100", ValidPassword),
            cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, repository.ReceivedCancellationToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_RejectsEmptyPasswordWithoutRepositoryCall(string? password)
    {
        var repository = new RecordingUserAccountRepository(null);
        var handler = CreateHandler(repository, passwordMatches: false);

        var result = await handler.Handle(
            new AuthenticateCommand(LoginType.Email, "user@example.com", password),
            CancellationToken.None);

        AssertInvalidCredentials(result);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task Handle_RejectsInvalidLoginWithoutRepositoryCall()
    {
        var repository = new RecordingUserAccountRepository(null);
        var handler = CreateHandler(repository, passwordMatches: false);

        var result = await handler.Handle(
            new AuthenticateCommand(LoginType.Email, "not-an-email", ValidPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidLogin.Code, result.Error.Code);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task Handle_ResultContainsNoCredentialsOrTokens()
    {
        var account = CreateAccount(UserRole.Director, LoginType.Email, "director@example.com");
        var handler = CreateHandler(
            new RecordingUserAccountRepository(account),
            passwordMatches: true);

        var result = await handler.Handle(
            new AuthenticateCommand(LoginType.Email, "director@example.com", ValidPassword),
            CancellationToken.None);
        var resultPropertyNames = typeof(AuthenticationResult)
            .GetProperties()
            .Select(property => property.Name)
            .Concat(result.Value.User.GetType().GetProperties().Select(property => property.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Password", resultPropertyNames);
        Assert.DoesNotContain("PasswordHash", resultPropertyNames);
        Assert.DoesNotContain("AccessToken", resultPropertyNames);
        Assert.DoesNotContain("RefreshToken", resultPropertyNames);
        Assert.DoesNotContain(ValidPassword, result.Value.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(StoredHash, result.Value.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AuthenticateCommand_ToStringDoesNotExposeCredentials()
    {
        var command = new AuthenticateCommand(
            LoginType.Email,
            "sensitive-login@example.com",
            "sensitive-password");

        Assert.DoesNotContain("sensitive-login@example.com", command.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-password", command.ToString(), StringComparison.Ordinal);
        Assert.Contains("REDACTED", command.ToString(), StringComparison.Ordinal);
    }

    private static AuthenticateHandler CreateHandler(
        RecordingUserAccountRepository repository,
        bool passwordMatches) => new(
            repository,
            new ConfigurablePasswordHasher(passwordMatches),
            new LoginNormalizer());

    private static UserAccount CreateAccount(
        UserRole role,
        LoginType loginType,
        string login)
    {
        var loginIdentity = LoginIdentity.Create(loginType, login).Value;
        return UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity,
            StoredHash,
            role,
            CreatedAt).Value;
    }

    private static void AssertInvalidCredentials(
        EcoBilling.SharedKernel.Results.Result<AuthenticationResult> result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidCredentials, result.Error);
    }

    private sealed class RecordingUserAccountRepository(UserAccount? account)
        : IUserAccountRepository
    {
        public int CallCount { get; private set; }

        public LoginIdentity? ReceivedLoginIdentity { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<UserAccount?> GetByLoginAsync(
            LoginIdentity loginIdentity,
            CancellationToken cancellationToken)
        {
            CallCount++;
            ReceivedLoginIdentity = loginIdentity;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(account);
        }
    }

    private sealed class ConfigurablePasswordHasher : IPasswordHasher
    {
        private readonly PasswordVerificationOutcome outcome;

        public ConfigurablePasswordHasher(bool passwordMatches)
            : this(passwordMatches
                ? PasswordVerificationOutcome.Success
                : PasswordVerificationOutcome.Failed)
        {
        }

        public ConfigurablePasswordHasher(PasswordVerificationOutcome outcome)
        {
            this.outcome = outcome;
        }

        public string Hash(string password) =>
            throw new NotSupportedException("Hashing is not used by authentication tests.");

        public PasswordVerificationOutcome Verify(string password, string passwordHash) =>
            password == ValidPassword && passwordHash == StoredHash
                ? outcome
                : PasswordVerificationOutcome.Failed;
    }
}
