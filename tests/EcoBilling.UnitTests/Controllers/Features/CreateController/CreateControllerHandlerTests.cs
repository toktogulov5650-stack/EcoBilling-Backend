using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.Modules.Controllers.Features.CreateController;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Controllers.Features.CreateController;

public sealed class CreateControllerHandlerTests
{
    private const string InitialCredential =
        "xTOHxQm8oC24bTWf9Uh5AF0w9Jm1mSIKzRFMtAlSHXo";
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WithValidRequest_CreatesControllerAtomically()
    {
        var repository = new RecordingRepository(
            ControllerCreationPersistenceOutcome.Created);
        var passwordHasher = new RecordingPasswordHasher();
        var handler = CreateHandler(repository, passwordHasher);

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsReplay);
        Assert.Equal(InitialCredential, passwordHasher.ReceivedPassword);
        Assert.Equal(UserRole.Controller, repository.UserAccount.Role);
        Assert.Equal(
            "CONTROLLER@EXAMPLE.COM",
            repository.UserAccount.LoginIdentity.NormalizedValue);
        Assert.True(repository.UserAccount.RequiresPasswordChange);
        Assert.Equal("stored-initial-credential-hash", repository.UserAccount.PasswordHash);
        Assert.Equal(UtcNow, repository.UserAccount.CreatedAt);
        Assert.Equal("Grace Hopper", repository.Controller.FullName);
        Assert.Equal(repository.UserAccount.Id, repository.Controller.UserId);
        Assert.Equal(
            "create-controller-1",
            repository.Operation.IdempotencyKey);
        Assert.DoesNotContain(
            InitialCredential,
            repository.Operation.RequestFingerprint,
            StringComparison.Ordinal);
        Assert.Equal(repository.Controller.Id, repository.Operation.ControllerId);
        Assert.Equal("director-user-id", repository.ActorId);
        Assert.Equal("trace-id", repository.CorrelationId);
    }

    [Fact]
    public async Task Handle_WhenRequestIsReplayed_ReturnsStoredIdentifiers()
    {
        var storedControllerId = new ControllerId(Guid.NewGuid());
        var storedOperationId = new ControllerCreationOperationId(Guid.NewGuid());
        var handler = CreateHandler(
            new RecordingRepository(
                ControllerCreationPersistenceOutcome.Replayed,
                storedControllerId,
                storedOperationId),
            new RecordingPasswordHasher());

        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsReplay);
        Assert.Equal(storedControllerId, result.Value.ControllerId);
        Assert.Equal(storedOperationId, result.Value.OperationId);
    }

    [Theory]
    [InlineData(
        ControllerCreationPersistenceOutcome.IdempotencyConflict,
        "controller.creation.idempotency_conflict")]
    [InlineData(
        ControllerCreationPersistenceOutcome.EmailAlreadyExists,
        "controller.email_already_exists")]
    public async Task Handle_MapsPersistenceConflict(
        ControllerCreationPersistenceOutcome outcome,
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
    [InlineData(null, "Grace Hopper", "controller@example.com", InitialCredential,
        "controller.creation.invalid_idempotency_key")]
    [InlineData("key", null, "controller@example.com", InitialCredential,
        "controller.invalid_full_name")]
    [InlineData("key", "Grace Hopper", "not-an-email", InitialCredential,
        "auth.invalid_login")]
    [InlineData("key", "Grace Hopper", "controller@example.com", "short",
        "controller.invalid_initial_credential")]
    public async Task Handle_WithInvalidRequest_DoesNotHashOrPersist(
        string? idempotencyKey,
        string? fullName,
        string? email,
        string? initialCredential,
        string expectedCode)
    {
        var repository = new RecordingRepository(
            ControllerCreationPersistenceOutcome.Created);
        var passwordHasher = new RecordingPasswordHasher();
        var handler = CreateHandler(repository, passwordHasher);

        var result = await handler.Handle(
            new CreateControllerCommand(
                idempotencyKey,
                fullName,
                email,
                initialCredential,
                "director-user-id",
                "trace-id"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Null(passwordHasher.ReceivedPassword);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public void Command_ToStringRedactsAllInput()
    {
        var text = CreateCommand().ToString();

        Assert.DoesNotContain("create-controller-1", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Grace Hopper", text, StringComparison.Ordinal);
        Assert.DoesNotContain("controller@example.com", text, StringComparison.Ordinal);
        Assert.DoesNotContain(InitialCredential, text, StringComparison.Ordinal);
        Assert.DoesNotContain("director-user-id", text, StringComparison.Ordinal);
        Assert.DoesNotContain("trace-id", text, StringComparison.Ordinal);
        Assert.Contains("REDACTED", text, StringComparison.Ordinal);
    }

    private static CreateControllerCommand CreateCommand() =>
        new(
            "create-controller-1",
            "  Grace Hopper  ",
            "  Controller@Example.com  ",
            InitialCredential,
            "director-user-id",
            "trace-id");

    private static CreateControllerHandler CreateHandler(
        RecordingRepository repository,
        RecordingPasswordHasher passwordHasher) =>
        new(
            repository,
            new StubFingerprinter(),
            passwordHasher,
            new FixedTimeProvider(UtcNow));

    private sealed class RecordingRepository(
        ControllerCreationPersistenceOutcome outcome,
        ControllerId? controllerId = null,
        ControllerCreationOperationId? operationId = null)
        : IControllerCreationRepository
    {
        public int CallCount { get; private set; }

        public UserAccount UserAccount { get; private set; } = null!;

        public Controller Controller { get; private set; } = null!;

        public ControllerCreationOperation Operation { get; private set; } = null!;

        public string ActorId { get; private set; } = string.Empty;

        public string CorrelationId { get; private set; } = string.Empty;

        public Task<ControllerCreationPersistenceResult> CreateAsync(
            UserAccount userAccount,
            Controller controller,
            ControllerCreationOperation operation,
            string actorId,
            string correlationId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            UserAccount = userAccount;
            Controller = controller;
            Operation = operation;
            ActorId = actorId;
            CorrelationId = correlationId;
            return Task.FromResult(
                new ControllerCreationPersistenceResult(
                    outcome,
                    controllerId,
                    operationId));
        }
    }

    private sealed class StubFingerprinter : IControllerCreationRequestFingerprinter
    {
        public string Create(
            string fullName,
            string normalizedEmail,
            string initialCredential) =>
            "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
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

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
