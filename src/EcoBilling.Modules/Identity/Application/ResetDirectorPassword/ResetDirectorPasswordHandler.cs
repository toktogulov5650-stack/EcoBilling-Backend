using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.PasswordSetup;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Identity.Application.ResetDirectorPassword;

public sealed class ResetDirectorPasswordHandler(
    IDirectorPasswordResetRepository repository,
    IDirectorPasswordResetRequestFingerprinter requestFingerprinter,
    IPasswordHasher passwordHasher,
    PasswordPolicy passwordPolicy,
    TimeProvider timeProvider)
{
    public async Task<Result<ResetDirectorPasswordResult>> Handle(
        ResetDirectorPasswordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        if (!passwordPolicy.IsValid(command.NewPassword))
        {
            return Result<ResetDirectorPasswordResult>.Failure(
                DirectorProvisioningErrors.InvalidPassword);
        }

        var loginIdentity = LoginIdentity.Create(LoginType.Email, command.Email);
        if (loginIdentity.IsFailure)
        {
            return Result<ResetDirectorPasswordResult>.Failure(loginIdentity.Error);
        }

        var operation = DirectorPasswordResetOperation.Create(
            new DirectorPasswordResetOperationId(Guid.NewGuid()),
            command.IdempotencyKey,
            requestFingerprinter.Create(
                loginIdentity.Value.NormalizedValue,
                command.NewPassword!),
            loginIdentity.Value.NormalizedValue,
            timeProvider.GetUtcNow());
        if (operation.IsFailure)
        {
            return Result<ResetDirectorPasswordResult>.Failure(operation.Error);
        }

        var persistenceResult = await repository.ResetAsync(
            operation.Value,
            passwordHasher.Hash(command.NewPassword!),
            command.ActorId,
            command.CorrelationId,
            cancellationToken);

        return persistenceResult.Outcome switch
        {
            DirectorPasswordResetPersistenceOutcome.Reset => Success(
                operation.Value.Id,
                isReplay: false),
            DirectorPasswordResetPersistenceOutcome.Replayed => Success(
                persistenceResult.OperationId!,
                isReplay: true),
            DirectorPasswordResetPersistenceOutcome.IdempotencyConflict =>
                Result<ResetDirectorPasswordResult>.Failure(
                    DirectorProvisioningErrors.IdempotencyConflict),
            DirectorPasswordResetPersistenceOutcome.DirectorNotFound =>
                Result<ResetDirectorPasswordResult>.Failure(
                    DirectorProvisioningErrors.DirectorNotFound),
            _ => throw new InvalidOperationException(
                $"Unknown director password reset outcome: {persistenceResult.Outcome}.")
        };
    }

    private static Result<ResetDirectorPasswordResult> Success(
        DirectorPasswordResetOperationId operationId,
        bool isReplay) =>
        Result<ResetDirectorPasswordResult>.Success(
            new ResetDirectorPasswordResult(operationId, isReplay));
}
