using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.PasswordSetup;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.Modules.Residents.Features.ResetPassword;

namespace EcoBilling.UnitTests.Residents.Features.ResetPassword;

public sealed class ResetResidentPasswordHandlerTests
{
    private const string NewPassword = "new-resident-password-2026!";
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WithValidRequest_HashesAndPersistsReset()
    {
        var repository = new RecordingRepository(
            ResidentPasswordResetPersistenceOutcome.Reset);
        var passwordHasher = new RecordingPasswordHasher();
        var handler = CreateHandler(repository, passwordHasher);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsReplay);
        Assert.Equal(NewPassword, passwordHasher.ReceivedPassword);
        Assert.Equal("stored-new-password-hash", repository.NewPasswordHash);
        Assert.Equal("reset-resident-password-1", repository.Operation.IdempotencyKey);
        Assert.Equal(UtcNow, repository.Operation.CreatedAt);
        Assert.Equal("director-user-id", repository.ActorId);
        Assert.Equal("trace-id", repository.CorrelationId);
        Assert.Equal(repository.Operation.Id, result.Value.OperationId);
    }

    [Fact]
    public async Task Handle_WhenRequestIsReplayed_ReturnsStoredOperationId()
    {
        var storedOperationId = new ResidentPasswordResetOperationId(Guid.NewGuid());
        var handler = CreateHandler(
            new RecordingRepository(
                ResidentPasswordResetPersistenceOutcome.Replayed,
                storedOperationId),
            new RecordingPasswordHasher());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsReplay);
        Assert.Equal(storedOperationId, result.Value.OperationId);
    }

    [Theory]
    [InlineData(
        ResidentPasswordResetPersistenceOutcome.IdempotencyConflict,
        "resident.password_reset.idempotency_conflict")]
    [InlineData(
        ResidentPasswordResetPersistenceOutcome.ResidentNotFound,
        "resident.not_found")]
    public async Task Handle_MapsPersistenceOutcome(
        ResidentPasswordResetPersistenceOutcome outcome,
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
    [InlineData(null, NewPassword, "resident.password_reset.invalid_idempotency_key")]
    [InlineData("key", null, "resident.invalid_password")]
    [InlineData("key", "short", "resident.invalid_password")]
    public async Task Handle_WithInvalidRequest_DoesNotHashOrPersist(
        string? idempotencyKey,
        string? newPassword,
        string expectedCode)
    {
        var repository = new RecordingRepository(
            ResidentPasswordResetPersistenceOutcome.Reset);
        var passwordHasher = new RecordingPasswordHasher();
        var handler = CreateHandler(repository, passwordHasher);

        var result = await handler.Handle(
            new ResetResidentPasswordCommand(
                new ResidentId(Guid.NewGuid()),
                idempotencyKey,
                newPassword,
                "director-user-id",
                "trace-id"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Null(passwordHasher.ReceivedPassword);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public void Command_ToStringRedactsSecretsAndOperationalMetadata()
    {
        var command = CreateCommand();
        var text = command.ToString();

        Assert.Contains(command.ResidentId.Value.ToString("D"), text, StringComparison.Ordinal);
        Assert.DoesNotContain("reset-resident-password-1", text, StringComparison.Ordinal);
        Assert.DoesNotContain(NewPassword, text, StringComparison.Ordinal);
        Assert.DoesNotContain("director-user-id", text, StringComparison.Ordinal);
        Assert.DoesNotContain("trace-id", text, StringComparison.Ordinal);
        Assert.Contains("REDACTED", text, StringComparison.Ordinal);
    }

    private static ResetResidentPasswordCommand CreateCommand() =>
        new(
            new ResidentId(Guid.NewGuid()),
            "reset-resident-password-1",
            NewPassword,
            "director-user-id",
            "trace-id");

    private static ResetResidentPasswordHandler CreateHandler(
        RecordingRepository repository,
        RecordingPasswordHasher passwordHasher) =>
        new(
            repository,
            new StubFingerprinter(),
            passwordHasher,
            PasswordPolicy.Default,
            new FixedTimeProvider(UtcNow));

    private sealed class RecordingRepository(
        ResidentPasswordResetPersistenceOutcome outcome,
        ResidentPasswordResetOperationId? operationId = null)
        : IResidentPasswordResetRepository
    {
        public int CallCount { get; private set; }
        public ResidentPasswordResetOperation Operation { get; private set; } = null!;
        public string NewPasswordHash { get; private set; } = string.Empty;
        public string ActorId { get; private set; } = string.Empty;
        public string CorrelationId { get; private set; } = string.Empty;

        public Task<ResidentPasswordResetPersistenceResult> ResetAsync(
            ResidentPasswordResetOperation operation,
            string newPasswordHash,
            string actorId,
            string correlationId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Operation = operation;
            NewPasswordHash = newPasswordHash;
            ActorId = actorId;
            CorrelationId = correlationId;
            return Task.FromResult(
                new ResidentPasswordResetPersistenceResult(outcome, operationId));
        }
    }

    private sealed class StubFingerprinter : IResidentPasswordResetRequestFingerprinter
    {
        public string Create(ResidentId residentId, string newPassword) =>
            "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
    }

    private sealed class RecordingPasswordHasher : IPasswordHasher
    {
        public string? ReceivedPassword { get; private set; }

        public string Hash(string password)
        {
            ReceivedPassword = password;
            return "stored-new-password-hash";
        }

        public PasswordVerificationOutcome Verify(string password, string passwordHash) =>
            throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
