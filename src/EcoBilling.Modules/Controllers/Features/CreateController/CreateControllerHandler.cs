using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.Credentials;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Controllers.Features.CreateController;

public sealed class CreateControllerHandler
{
    private readonly IControllerCreationRepository repository;
    private readonly IControllerCreationRequestFingerprinter requestFingerprinter;
    private readonly IPasswordHasher passwordHasher;
    private readonly TimeProvider timeProvider;

    public CreateControllerHandler(
        IControllerCreationRepository repository,
        IControllerCreationRequestFingerprinter requestFingerprinter,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider)
    {
        this.repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        this.requestFingerprinter = requestFingerprinter
            ?? throw new ArgumentNullException(nameof(requestFingerprinter));
        this.passwordHasher = passwordHasher
            ?? throw new ArgumentNullException(nameof(passwordHasher));
        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<CreateControllerResult>> Handle(
        CreateControllerCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) ||
            command.IdempotencyKey.Length >
            ControllerCreationOperation.MaximumIdempotencyKeyLength)
        {
            return Result<CreateControllerResult>.Failure(
                ControllerErrors.InvalidCreationIdempotencyKey);
        }

        if (!InitialCredentialPolicy.IsValid(command.InitialCredential))
        {
            return Result<CreateControllerResult>.Failure(
                ControllerErrors.InvalidInitialCredential);
        }

        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            return Result<CreateControllerResult>.Failure(
                ControllerErrors.InvalidFullName);
        }

        var loginIdentity = LoginIdentity.Create(LoginType.Email, command.Email);
        if (loginIdentity.IsFailure)
        {
            return Result<CreateControllerResult>.Failure(loginIdentity.Error);
        }

        var createdAt = timeProvider.GetUtcNow();
        var userAccount = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity.Value,
            passwordHasher.Hash(command.InitialCredential!),
            UserRole.Controller,
            createdAt,
            requiresPasswordChange: true);
        if (userAccount.IsFailure)
        {
            return Result<CreateControllerResult>.Failure(userAccount.Error);
        }

        var controller = Controller.Create(
            new ControllerId(Guid.NewGuid()),
            userAccount.Value,
            command.FullName,
            createdAt);
        if (controller.IsFailure)
        {
            return Result<CreateControllerResult>.Failure(controller.Error);
        }

        var operation = ControllerCreationOperation.Create(
            new ControllerCreationOperationId(Guid.NewGuid()),
            command.IdempotencyKey,
            requestFingerprinter.Create(
                controller.Value.FullName,
                loginIdentity.Value.NormalizedValue,
                command.InitialCredential!),
            controller.Value.Id,
            createdAt);
        if (operation.IsFailure)
        {
            return Result<CreateControllerResult>.Failure(operation.Error);
        }

        var persistenceResult = await repository.CreateAsync(
            userAccount.Value,
            controller.Value,
            operation.Value,
            command.ActorId,
            command.CorrelationId,
            cancellationToken);

        return persistenceResult.Outcome switch
        {
            ControllerCreationPersistenceOutcome.Created => Success(
                operation.Value.ControllerId,
                operation.Value.Id,
                isReplay: false),
            ControllerCreationPersistenceOutcome.Replayed => Success(
                persistenceResult.ControllerId!,
                persistenceResult.OperationId!,
                isReplay: true),
            ControllerCreationPersistenceOutcome.IdempotencyConflict =>
                Result<CreateControllerResult>.Failure(
                    ControllerErrors.CreationIdempotencyConflict),
            ControllerCreationPersistenceOutcome.EmailAlreadyExists =>
                Result<CreateControllerResult>.Failure(
                    ControllerErrors.EmailAlreadyExists),
            _ => throw new InvalidOperationException(
                $"Unknown controller creation outcome: {persistenceResult.Outcome}.")
        };
    }

    private static Result<CreateControllerResult> Success(
        ControllerId controllerId,
        ControllerCreationOperationId operationId,
        bool isReplay) =>
        Result<CreateControllerResult>.Success(
            new CreateControllerResult(controllerId, operationId, isReplay));
}
