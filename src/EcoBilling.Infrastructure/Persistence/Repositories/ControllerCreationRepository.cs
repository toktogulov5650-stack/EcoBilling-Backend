using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class ControllerCreationRepository(EcoBillingDbContext dbContext)
    : IControllerCreationRepository
{
    private const long CreationLockId = 3_204_029_116_771_503_121;

    public async Task<ControllerCreationPersistenceResult> CreateAsync(
        UserAccount userAccount,
        Controller controller,
        ControllerCreationOperation operation,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userAccount);
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({CreationLockId})",
            cancellationToken);

        var existingOperation = await dbContext.ControllerCreationOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existing => existing.IdempotencyKey == operation.IdempotencyKey,
                cancellationToken);

        if (existingOperation is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existingOperation.RequestFingerprint == operation.RequestFingerprint
                ? new ControllerCreationPersistenceResult(
                    ControllerCreationPersistenceOutcome.Replayed,
                    existingOperation.ControllerId,
                    existingOperation.Id)
                : new ControllerCreationPersistenceResult(
                    ControllerCreationPersistenceOutcome.IdempotencyConflict);
        }

        var conflictingAccountExists = await dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(
                existing =>
                    existing.LoginIdentity.Type == userAccount.LoginIdentity.Type &&
                    existing.LoginIdentity.NormalizedValue ==
                    userAccount.LoginIdentity.NormalizedValue,
                cancellationToken);

        if (conflictingAccountExists)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ControllerCreationPersistenceResult(
                ControllerCreationPersistenceOutcome.EmailAlreadyExists);
        }

        dbContext.UserAccounts.Add(userAccount);
        dbContext.Controllers.Add(controller);
        dbContext.ControllerCreationOperations.Add(operation);
        dbContext.AuditLogs.Add(
            CreateAudit(userAccount, controller, operation, actorId, correlationId));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ControllerCreationPersistenceResult(
            ControllerCreationPersistenceOutcome.Created,
            controller.Id,
            operation.Id);
    }

    private static AuditLog CreateAudit(
        UserAccount userAccount,
        Controller controller,
        ControllerCreationOperation operation,
        string actorId,
        string correlationId) =>
        new(
            Guid.NewGuid(),
            "User",
            actorId,
            "controllers.controller.created",
            "Controller",
            controller.Id.Value.ToString("D"),
            beforeData: null,
            JsonSerializer.Serialize(
                new
                {
                    controllerId = controller.Id.Value,
                    userId = userAccount.Id.Value,
                    operationId = operation.Id.Value
                }),
            correlationId,
            operation.CreatedAt);
}
