using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class DirectorProvisioningRepository(EcoBillingDbContext dbContext)
    : IDirectorProvisioningRepository
{
    private const long ProvisioningLockId = 2_603_019_332_451_401_447;

    public async Task<DirectorProvisioningPersistenceResult> ProvisionAsync(
        UserAccount userAccount,
        DirectorProfile director,
        DirectorProvisioningOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userAccount);
        ArgumentNullException.ThrowIfNull(director);
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({ProvisioningLockId})",
            cancellationToken);

        var existingOperation = await dbContext.DirectorProvisioningOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existing => existing.IdempotencyKey == operation.IdempotencyKey,
                cancellationToken);

        if (existingOperation is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existingOperation.RequestFingerprint == operation.RequestFingerprint
                ? new DirectorProvisioningPersistenceResult(
                    DirectorProvisioningPersistenceOutcome.Replayed,
                    existingOperation.DirectorId,
                    existingOperation.Id)
                : new DirectorProvisioningPersistenceResult(
                    DirectorProvisioningPersistenceOutcome.IdempotencyConflict);
        }

        var directorProfileExists = await dbContext.Directors
            .AsNoTracking()
            .AnyAsync(cancellationToken);
        var conflictingAccountExists = await dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(
                existing =>
                    existing.Role == UserRole.Director ||
                    (existing.LoginIdentity.Type == userAccount.LoginIdentity.Type &&
                     existing.LoginIdentity.NormalizedValue ==
                     userAccount.LoginIdentity.NormalizedValue),
                cancellationToken);

        if (directorProfileExists || conflictingAccountExists)
        {
            await transaction.CommitAsync(cancellationToken);
            return new DirectorProvisioningPersistenceResult(
                DirectorProvisioningPersistenceOutcome.DirectorAlreadyExists);
        }

        dbContext.UserAccounts.Add(userAccount);
        dbContext.Directors.Add(director);
        dbContext.DirectorProvisioningOperations.Add(operation);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DirectorProvisioningPersistenceResult(
            DirectorProvisioningPersistenceOutcome.Created,
            director.Id,
            operation.Id);
    }
}
