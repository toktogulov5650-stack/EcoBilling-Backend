using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Repositories;

public sealed class ResidentCreationRepository(EcoBillingDbContext dbContext)
    : IResidentCreationRepository
{
    private const long CreationLockId = 5_091_142_804_113_761_009;

    public async Task<ResidentCreationPersistenceResult> CreateAsync(
        UserAccount userAccount,
        Resident resident,
        Address address,
        Account account,
        ResidentCreationOperation operation,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userAccount);
        ArgumentNullException.ThrowIfNull(resident);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({CreationLockId})",
            cancellationToken);

        var existingOperation = await dbContext.ResidentCreationOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existing => existing.IdempotencyKey == operation.IdempotencyKey,
                cancellationToken);
        if (existingOperation is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existingOperation.RequestFingerprint == operation.RequestFingerprint
                ? new ResidentCreationPersistenceResult(
                    ResidentCreationPersistenceOutcome.Replayed,
                    existingOperation.ResidentId,
                    existingOperation.AccountId,
                    existingOperation.AddressId,
                    existingOperation.Id)
                : new ResidentCreationPersistenceResult(
                    ResidentCreationPersistenceOutcome.IdempotencyConflict);
        }

        var conflictingIdentity = await dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(
                existing =>
                    existing.LoginIdentity.Type == userAccount.LoginIdentity.Type &&
                    existing.LoginIdentity.NormalizedValue ==
                    userAccount.LoginIdentity.NormalizedValue,
                cancellationToken);
        var conflictingAccount = await dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(existing => existing.Number == account.Number, cancellationToken);
        if (conflictingIdentity || conflictingAccount)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ResidentCreationPersistenceResult(
                ResidentCreationPersistenceOutcome.AccountNumberAlreadyExists);
        }

        dbContext.UserAccounts.Add(userAccount);
        dbContext.Residents.Add(resident);
        dbContext.Addresses.Add(address);
        dbContext.Accounts.Add(account);
        dbContext.ResidentCreationOperations.Add(operation);
        dbContext.AuditLogs.Add(
            CreateAudit(
                userAccount,
                resident,
                address,
                account,
                operation,
                actorId,
                correlationId));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ResidentCreationPersistenceResult(
            ResidentCreationPersistenceOutcome.Created,
            resident.Id,
            account.Id,
            address.Id,
            operation.Id);
    }

    private static AuditLog CreateAudit(
        UserAccount userAccount,
        Resident resident,
        Address address,
        Account account,
        ResidentCreationOperation operation,
        string actorId,
        string correlationId) =>
        new(
            Guid.NewGuid(),
            "User",
            actorId,
            "residents.resident.created",
            "Resident",
            resident.Id.Value.ToString("D"),
            beforeData: null,
            JsonSerializer.Serialize(
                new
                {
                    residentId = resident.Id.Value,
                    userId = userAccount.Id.Value,
                    accountId = account.Id.Value,
                    addressId = address.Id.Value,
                    operationId = operation.Id.Value
                }),
            correlationId,
            operation.CreatedAt);
}
