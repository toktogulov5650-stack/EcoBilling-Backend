using EcoBilling.Modules.Identity.Application;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.Authenticate;
using EcoBilling.Modules.Identity.Application.PasswordSetup;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Application;

public sealed class SetInitialPasswordHandlerTests
{
    private const string InitialCredential = "initial-credential-value";
    private const string NewPassword = "new-strong-password";
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 28, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ReplacesInitialCredentialForStaffAccount()
    {
        var account = CreateAccount(UserRole.Director, requiresPasswordChange: true);
        var repository = new RecordingRepository(account);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(account.RequiresPasswordChange);
        Assert.Equal("hash:new-strong-password", account.PasswordHash);
        Assert.Equal(1, repository.SaveCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short")]
    [InlineData(InitialCredential)]
    public async Task Handle_RejectsInvalidOrUnchangedNewPassword(string? password)
    {
        var repository = new RecordingRepository(
            CreateAccount(UserRole.Controller, requiresPasswordChange: true));
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            CreateCommand(newPassword: password),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidPassword, result.Error);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Handle_WrongInitialCredentialRecordsFailedAttempt()
    {
        var account = CreateAccount(UserRole.Controller, requiresPasswordChange: true);
        var repository = new RecordingRepository(account);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            CreateCommand(initialCredential: "wrong-credential"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidCredentials, result.Error);
        Assert.Equal(1, account.FailedLoginAttempts);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Handle_ResidentCannotUseSelfServicePasswordSetup()
    {
        var account = CreateAccount(UserRole.Resident, requiresPasswordChange: true);
        var repository = new RecordingRepository(account);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new SetInitialPasswordCommand(
                LoginType.AccountNumber,
                "A-100",
                InitialCredential,
                NewPassword),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.PasswordSetupNotAvailable, result.Error);
        Assert.True(account.RequiresPasswordChange);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public void Command_ToStringRedactsAllCredentials()
    {
        var text = CreateCommand().ToString();

        Assert.DoesNotContain("director@example.com", text, StringComparison.Ordinal);
        Assert.DoesNotContain(InitialCredential, text, StringComparison.Ordinal);
        Assert.DoesNotContain(NewPassword, text, StringComparison.Ordinal);
    }

    private static SetInitialPasswordHandler CreateHandler(RecordingRepository repository) =>
        new(
            repository,
            new StubPasswordHasher(),
            new LoginNormalizer(),
            PasswordPolicy.Default,
            AuthenticationPolicy.Default,
            new FixedTimeProvider(UtcNow));

    private static SetInitialPasswordCommand CreateCommand(
        string? initialCredential = InitialCredential,
        string? newPassword = NewPassword) =>
        new(LoginType.Email, "director@example.com", initialCredential, newPassword);

    private static UserAccount CreateAccount(
        UserRole role,
        bool requiresPasswordChange)
    {
        var loginType = role is UserRole.Resident
            ? LoginType.AccountNumber
            : LoginType.Email;
        var login = role is UserRole.Resident
            ? "A-100"
            : "director@example.com";
        return UserAccount.Create(
            new UserId(Guid.NewGuid()),
            LoginIdentity.Create(loginType, login).Value,
            $"hash:{InitialCredential}",
            role,
            UtcNow,
            requiresPasswordChange).Value;
    }

    private sealed class RecordingRepository(UserAccount? account) : IUserAccountRepository
    {
        public int SaveCount { get; private set; }

        public Task<UserAccount?> GetByLoginAsync(
            LoginIdentity loginIdentity,
            CancellationToken cancellationToken) => Task.FromResult(account);

        public Task SaveAsync(UserAccount userAccount, CancellationToken cancellationToken)
        {
            Assert.Same(account, userAccount);
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash:{password}";

        public PasswordVerificationOutcome Verify(string password, string passwordHash) =>
            passwordHash == $"hash:{password}"
                ? PasswordVerificationOutcome.Success
                : PasswordVerificationOutcome.Failed;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
