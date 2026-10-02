using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.PasswordSetup;
using EcoBilling.Modules.Identity.Application.ResetDirectorPassword;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Application;

public sealed class ResetDirectorPasswordHandlerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WithValidRequest_HashesPasswordAndCompletesReset()
    {
        var repository = new RecordingRepository(
            DirectorPasswordResetPersistenceOutcome.Reset);
        var passwordHasher = new RecordingPasswordHasher();
        var handler = CreateHandler(repository, passwordHasher);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsReplay);
        Assert.Equal("administrator-selected-password", passwordHasher.Password);
        Assert.Equal("DIRECTOR@EXAMPLE.COM", repository.Operation.NormalizedEmail);
        Assert.Equal("stored-password-hash", repository.PasswordHash);
        Assert.Equal("ecobilling-control", repository.ActorId);
        Assert.Equal("trace-id", repository.CorrelationId);
    }

    [Theory]
    [InlineData(DirectorPasswordResetPersistenceOutcome.IdempotencyConflict, "operation.idempotency_conflict")]
    [InlineData(DirectorPasswordResetPersistenceOutcome.DirectorNotFound, "director.not_found")]
    public async Task Handle_MapsPersistenceFailures(
        DirectorPasswordResetPersistenceOutcome outcome,
        string expectedCode)
    {
        var handler = CreateHandler(
            new RecordingRepository(outcome),
            new RecordingPasswordHasher());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public async Task Handle_RejectsInvalidPasswordBeforeCallingRepository()
    {
        var repository = new RecordingRepository(
            DirectorPasswordResetPersistenceOutcome.Reset);
        var handler = CreateHandler(repository, new RecordingPasswordHasher());

        var result = await handler.Handle(
            CreateCommand(newPassword: "short"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("director.invalid_password", result.Error.Code);
        Assert.Equal(0, repository.CallCount);
    }

    private static ResetDirectorPasswordCommand CreateCommand(
        string newPassword = "administrator-selected-password") =>
        new(
            "reset-director-1",
            " director@example.com ",
            newPassword,
            "ecobilling-control",
            "trace-id");

    private static ResetDirectorPasswordHandler CreateHandler(
        RecordingRepository repository,
        RecordingPasswordHasher passwordHasher) =>
        new(
            repository,
            new StubFingerprinter(),
            passwordHasher,
            PasswordPolicy.Default,
            new FixedTimeProvider(UtcNow));

    private sealed class RecordingRepository(
        DirectorPasswordResetPersistenceOutcome outcome)
        : IDirectorPasswordResetRepository
    {
        public int CallCount { get; private set; }

        public DirectorPasswordResetOperation Operation { get; private set; } = null!;

        public string PasswordHash { get; private set; } = string.Empty;

        public string ActorId { get; private set; } = string.Empty;

        public string CorrelationId { get; private set; } = string.Empty;

        public Task<DirectorPasswordResetPersistenceResult> ResetAsync(
            DirectorPasswordResetOperation operation,
            string newPasswordHash,
            string actorId,
            string correlationId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Operation = operation;
            PasswordHash = newPasswordHash;
            ActorId = actorId;
            CorrelationId = correlationId;
            return Task.FromResult(
                new DirectorPasswordResetPersistenceResult(outcome, operation.Id));
        }
    }

    private sealed class StubFingerprinter : IDirectorPasswordResetRequestFingerprinter
    {
        public string Create(string normalizedEmail, string newPassword) =>
            "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
    }

    private sealed class RecordingPasswordHasher : IPasswordHasher
    {
        public string? Password { get; private set; }

        public string Hash(string password)
        {
            Password = password;
            return "stored-password-hash";
        }

        public PasswordVerificationOutcome Verify(string password, string passwordHash) =>
            throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
