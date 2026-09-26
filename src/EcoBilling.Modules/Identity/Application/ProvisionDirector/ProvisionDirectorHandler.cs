using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Application.ProvisionDirector;

public sealed class ProvisionDirectorHandler
{
    public const int MinimumInitialCredentialLength = 32;
    public const int MaximumInitialCredentialLength = 256;

    private readonly IDirectorProvisioningRepository repository;
    private readonly IDirectorProvisioningRequestFingerprinter requestFingerprinter;
    private readonly IPasswordHasher passwordHasher;
    private readonly TimeProvider timeProvider;

    public ProvisionDirectorHandler(
        IDirectorProvisioningRepository repository,
        IDirectorProvisioningRequestFingerprinter requestFingerprinter,
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

    public async Task<Result<ProvisionDirectorResult>> Handle(
        ProvisionDirectorCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) ||
            command.IdempotencyKey.Length >
            DirectorProvisioningOperation.MaximumIdempotencyKeyLength)
        {
            return Result<ProvisionDirectorResult>.Failure(
                DirectorProvisioningErrors.InvalidIdempotencyKey);
        }

        if (string.IsNullOrWhiteSpace(command.InitialCredential) ||
            command.InitialCredential.Length < MinimumInitialCredentialLength ||
            command.InitialCredential.Length > MaximumInitialCredentialLength ||
            command.InitialCredential.Any(char.IsWhiteSpace))
        {
            return Result<ProvisionDirectorResult>.Failure(
                DirectorProvisioningErrors.InvalidInitialCredential);
        }

        var loginIdentity = LoginIdentity.Create(LoginType.Email, command.Email);
        if (loginIdentity.IsFailure)
        {
            return Result<ProvisionDirectorResult>.Failure(loginIdentity.Error);
        }

        var createdAt = timeProvider.GetUtcNow();
        var userAccount = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity.Value,
            passwordHasher.Hash(command.InitialCredential),
            UserRole.Director,
            createdAt,
            requiresPasswordChange: true);
        if (userAccount.IsFailure)
        {
            return Result<ProvisionDirectorResult>.Failure(userAccount.Error);
        }

        var director = DirectorProfile.Create(
            new DirectorId(Guid.NewGuid()),
            userAccount.Value,
            command.FullName,
            createdAt);
        if (director.IsFailure)
        {
            return Result<ProvisionDirectorResult>.Failure(director.Error);
        }

        var operation = DirectorProvisioningOperation.Create(
            new DirectorProvisioningOperationId(Guid.NewGuid()),
            command.IdempotencyKey,
            requestFingerprinter.Create(
                director.Value.FullName,
                loginIdentity.Value.NormalizedValue,
                command.InitialCredential),
            director.Value.Id,
            createdAt);
        if (operation.IsFailure)
        {
            return Result<ProvisionDirectorResult>.Failure(operation.Error);
        }

        var persistenceResult = await repository.ProvisionAsync(
            userAccount.Value,
            director.Value,
            operation.Value,
            cancellationToken);

        return persistenceResult.Outcome switch
        {
            DirectorProvisioningPersistenceOutcome.Created => Success(
                operation.Value.DirectorId,
                operation.Value.Id,
                isReplay: false),
            DirectorProvisioningPersistenceOutcome.Replayed => Success(
                persistenceResult.DirectorId!,
                persistenceResult.OperationId!,
                isReplay: true),
            DirectorProvisioningPersistenceOutcome.IdempotencyConflict =>
                Result<ProvisionDirectorResult>.Failure(
                    DirectorProvisioningErrors.IdempotencyConflict),
            DirectorProvisioningPersistenceOutcome.DirectorAlreadyExists =>
                Result<ProvisionDirectorResult>.Failure(
                    DirectorProvisioningErrors.DirectorAlreadyExists),
            _ => throw new InvalidOperationException(
                $"Unknown director provisioning outcome: {persistenceResult.Outcome}.")
        };
    }

    private static Result<ProvisionDirectorResult> Success(
        DirectorId directorId,
        DirectorProvisioningOperationId operationId,
        bool isReplay) =>
        Result<ProvisionDirectorResult>.Success(
            new ProvisionDirectorResult(directorId, operationId, isReplay));

}
