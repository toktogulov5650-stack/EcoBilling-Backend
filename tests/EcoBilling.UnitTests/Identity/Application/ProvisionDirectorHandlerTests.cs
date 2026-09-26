using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.ProvisionDirector;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Application;

public sealed class ProvisionDirectorHandlerTests
{
    private const string InitialCredential =
        "xTOHxQm8oC24bTWf9Uh5AF0w9Jm1mSIKzRFMtAlSHXo";
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WithValidRequest_CreatesPendingDirectorAtomically()
    {
        var repository = new RecordingRepository(
            DirectorProvisioningPersistenceOutcome.Created);
        var passwordHasher = new RecordingPasswordHasher();
        var handler = CreateHandler(repository, passwordHasher);

        var result = await handler.Handle(
            CreateCommand(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsReplay);
        Assert.Equal(InitialCredential, passwordHasher.ReceivedPassword);
        Assert.NotNull(repository.UserAccount);
        Assert.Equal(UserRole.Director, repository.UserAccount.Role);
        Assert.Equal("DIRECTOR@EXAMPLE.COM", repository.UserAccount.LoginIdentity.NormalizedValue);
        Assert.True(repository.UserAccount.RequiresPasswordChange);
        Assert.Equal("stored-initial-credential-hash", repository.UserAccount.PasswordHash);
        Assert.Equal(UtcNow, repository.UserAccount.CreatedAt);
        Assert.NotNull(repository.Director);
        Assert.Equal("Ada Lovelace", repository.Director.FullName);
        Assert.Equal(repository.UserAccount.Id, repository.Director.UserId);
        Assert.NotNull(repository.Operation);
        Assert.Equal("provision-director-1", repository.Operation.IdempotencyKey);
        Assert.DoesNotContain(
            InitialCredential,
            repository.Operation.RequestFingerprint,
            StringComparison.Ordinal);
        Assert.Equal(repository.Director.Id, repository.Operation.DirectorId);
    }

    [Fact]
    public async Task Handle_WhenOperationIsReplayed_ReturnsStoredIdentifiers()
    {
        var storedDirectorId = new DirectorId(Guid.NewGuid());
        var storedOperationId = new DirectorProvisioningOperationId(Guid.NewGuid());
        var repository = new RecordingRepository(
            DirectorProvisioningPersistenceOutcome.Replayed,
            storedDirectorId,
            storedOperationId);
        var handler = CreateHandler(repository, new RecordingPasswordHasher());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsReplay);
        Assert.Equal(storedDirectorId, result.Value.DirectorId);
        Assert.Equal(storedOperationId, result.Value.OperationId);
    }

    [Theory]
    [InlineData(
        DirectorProvisioningPersistenceOutcome.IdempotencyConflict,
        "operation.idempotency_conflict")]
    [InlineData(
        DirectorProvisioningPersistenceOutcome.DirectorAlreadyExists,
        "director.already_exists")]
    public async Task Handle_MapsPersistenceConflicts(
        DirectorProvisioningPersistenceOutcome outcome,
        string expectedCode)
    {
        var handler = CreateHandler(
            new RecordingRepository(outcome),
            new RecordingPasswordHasher());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_RejectsMissingIdempotencyKeyBeforeHashing(string? key)
    {
        var repository = new RecordingRepository(
            DirectorProvisioningPersistenceOutcome.Created);
        var passwordHasher = new RecordingPasswordHasher();
        var handler = CreateHandler(repository, passwordHasher);

        var result = await handler.Handle(
            new ProvisionDirectorCommand(
                key,
                "Ada Lovelace",
                "director@example.com",
                InitialCredential),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DirectorProvisioningErrors.InvalidIdempotencyKey, result.Error);
        Assert.Null(passwordHasher.ReceivedPassword);
        Assert.Equal(0, repository.CallCount);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("credential containing spaces 1234567890")]
    public async Task Handle_RejectsInvalidInitialCredential(string credential)
    {
        var repository = new RecordingRepository(
            DirectorProvisioningPersistenceOutcome.Created);
        var handler = CreateHandler(repository, new RecordingPasswordHasher());

        var result = await handler.Handle(
            new ProvisionDirectorCommand(
                "operation-1",
                "Ada Lovelace",
                "director@example.com",
                credential),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DirectorProvisioningErrors.InvalidInitialCredential, result.Error);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public void Command_ToStringRedactsAllInput()
    {
        var command = CreateCommand();

        var text = command.ToString();

        Assert.DoesNotContain("provision-director-1", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Ada Lovelace", text, StringComparison.Ordinal);
        Assert.DoesNotContain("director@example.com", text, StringComparison.Ordinal);
        Assert.DoesNotContain(InitialCredential, text, StringComparison.Ordinal);
        Assert.Contains("REDACTED", text, StringComparison.Ordinal);
    }

    private static ProvisionDirectorCommand CreateCommand() =>
        new(
            "provision-director-1",
            "  Ada Lovelace  ",
            "  Director@Example.com  ",
            InitialCredential);

    private static ProvisionDirectorHandler CreateHandler(
        RecordingRepository repository,
        RecordingPasswordHasher passwordHasher) =>
        new(
            repository,
            new StubRequestFingerprinter(),
            passwordHasher,
            new FixedTimeProvider(UtcNow));

    private sealed class RecordingRepository(
        DirectorProvisioningPersistenceOutcome outcome,
        DirectorId? directorId = null,
        DirectorProvisioningOperationId? operationId = null)
        : IDirectorProvisioningRepository
    {
        public int CallCount { get; private set; }

        public UserAccount UserAccount { get; private set; } = null!;

        public DirectorProfile Director { get; private set; } = null!;

        public DirectorProvisioningOperation Operation { get; private set; } = null!;

        public Task<DirectorProvisioningPersistenceResult> ProvisionAsync(
            UserAccount userAccount,
            DirectorProfile director,
            DirectorProvisioningOperation operation,
            CancellationToken cancellationToken)
        {
            CallCount++;
            UserAccount = userAccount;
            Director = director;
            Operation = operation;
            return Task.FromResult(
                new DirectorProvisioningPersistenceResult(
                    outcome,
                    directorId,
                    operationId));
        }
    }

    private sealed class RecordingPasswordHasher : IPasswordHasher
    {
        public string? ReceivedPassword { get; private set; }

        public string Hash(string password)
        {
            ReceivedPassword = password;
            return "stored-initial-credential-hash";
        }

        public PasswordVerificationOutcome Verify(string password, string passwordHash) =>
            throw new NotSupportedException();
    }

    private sealed class StubRequestFingerprinter
        : IDirectorProvisioningRequestFingerprinter
    {
        public string Create(
            string fullName,
            string normalizedEmail,
            string initialCredential) =>
            "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
