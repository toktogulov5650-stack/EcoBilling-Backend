using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.PasswordSetup;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Residents.Features.ResetPassword;

public sealed class ResetResidentPasswordHandler
{
    private readonly IResidentPasswordResetRepository repository;
    private readonly IResidentPasswordResetRequestFingerprinter requestFingerprinter;
    private readonly IPasswordHasher passwordHasher;
    private readonly PasswordPolicy passwordPolicy;
    private readonly TimeProvider timeProvider;

    public ResetResidentPasswordHandler(
        IResidentPasswordResetRepository repository,
        IResidentPasswordResetRequestFingerprinter requestFingerprinter,
        IPasswordHasher passwordHasher,
        PasswordPolicy passwordPolicy,
        TimeProvider timeProvider)
    {
        this.repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        this.requestFingerprinter = requestFingerprinter
            ?? throw new ArgumentNullException(nameof(requestFingerprinter));
        this.passwordHasher = passwordHasher
            ?? throw new ArgumentNullException(nameof(passwordHasher));
        this.passwordPolicy = passwordPolicy
            ?? throw new ArgumentNullException(nameof(passwordPolicy));
        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<ResetResidentPasswordResult>> Handle(
        ResetResidentPasswordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) ||
            command.IdempotencyKey.Length >
            ResidentPasswordResetOperation.MaximumIdempotencyKeyLength)
        {
            return Result<ResetResidentPasswordResult>.Failure(
                ResidentErrors.InvalidPasswordResetIdempotencyKey);
        }

        if (!passwordPolicy.IsValid(command.NewPassword))
        {
            return Result<ResetResidentPasswordResult>.Failure(
                ResidentErrors.InvalidPassword);
        }

        var now = timeProvider.GetUtcNow();
        var operation = ResidentPasswordResetOperation.Create(
            new ResidentPasswordResetOperationId(Guid.NewGuid()),
            command.IdempotencyKey,
            requestFingerprinter.Create(command.ResidentId, command.NewPassword!),
            command.ResidentId,
            now);
        if (operation.IsFailure)
        {
            return Result<ResetResidentPasswordResult>.Failure(operation.Error);
        }

        var persistenceResult = await repository.ResetAsync(
            operation.Value,
            passwordHasher.Hash(command.NewPassword!),
            command.ActorId,
            command.CorrelationId,
            cancellationToken);

        return persistenceResult.Outcome switch
        {
            ResidentPasswordResetPersistenceOutcome.Reset => Success(
                operation.Value.Id,
                isReplay: false),
            ResidentPasswordResetPersistenceOutcome.Replayed => Success(
                persistenceResult.OperationId!,
                isReplay: true),
            ResidentPasswordResetPersistenceOutcome.IdempotencyConflict =>
                Result<ResetResidentPasswordResult>.Failure(
                    ResidentErrors.PasswordResetIdempotencyConflict),
            ResidentPasswordResetPersistenceOutcome.ResidentNotFound =>
                Result<ResetResidentPasswordResult>.Failure(ResidentErrors.NotFound),
            _ => throw new InvalidOperationException(
                $"Unknown resident password reset outcome: {persistenceResult.Outcome}.")
        };
    }

    private static Result<ResetResidentPasswordResult> Success(
        ResidentPasswordResetOperationId operationId,
        bool isReplay) =>
        Result<ResetResidentPasswordResult>.Success(
            new ResetResidentPasswordResult(operationId, isReplay));
}
