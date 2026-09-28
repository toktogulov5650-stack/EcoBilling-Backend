using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.IntegrationTests.Residents;

public sealed class ResidentCreationPersistenceTests
{
    private const string Fingerprint =
        "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
    private const string ActorId = "director-user-id";
    private const string CorrelationId = "trace-resident-creation";

    [PostgreSqlFact]
    public async Task CreateAsync_CreatesAggregateOperationAndAuditAtomically()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var input = CreateInput("operation-1", Fingerprint, "AB-000001");

        var result = await PersistAsync(context, input, CorrelationId);

        Assert.Equal(ResidentCreationPersistenceOutcome.Created, result.Outcome);
        var storedIdentity = await context.UserAccounts.AsNoTracking().SingleAsync();
        var storedResident = await context.Residents.AsNoTracking().SingleAsync();
        var storedAddress = await context.Addresses.AsNoTracking().SingleAsync();
        var storedAccount = await context.Accounts.AsNoTracking().SingleAsync();
        var storedOperation = await context.ResidentCreationOperations
            .AsNoTracking()
            .SingleAsync();
        var storedAudit = await context.AuditLogs.AsNoTracking().SingleAsync();
        Assert.False(storedIdentity.RequiresPasswordChange);
        Assert.Equal(UserRole.Resident, storedIdentity.Role);
        Assert.Equal(input.Resident.Id, storedResident.Id);
        Assert.Equal(input.Address.Id, storedAddress.Id);
        Assert.Equal(input.Account.Id, storedAccount.Id);
        Assert.Equal(input.Operation.Id, storedOperation.Id);
        Assert.Equal(input.Resident.Id, storedOperation.ResidentId);
        Assert.Equal(input.Account.Id, storedOperation.AccountId);
        Assert.Equal(input.Address.Id, storedOperation.AddressId);
        Assert.Equal("User", storedAudit.ActorType);
        Assert.Equal(ActorId, storedAudit.ActorId);
        Assert.Equal("residents.resident.created", storedAudit.Action);
        Assert.Equal("Resident", storedAudit.EntityType);
        Assert.Equal(input.Resident.Id.Value.ToString("D"), storedAudit.EntityId);
        Assert.Null(storedAudit.BeforeData);
        Assert.NotNull(storedAudit.AfterData);
        Assert.Contains(
            input.Operation.Id.Value.ToString("D"),
            storedAudit.AfterData,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            input.UserAccount.PasswordHash,
            storedAudit.AfterData,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "AB-000001",
            storedAudit.AfterData,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(CorrelationId, storedAudit.CorrelationId);
    }

    [PostgreSqlFact]
    public async Task CreateAsync_WithSameKeyAndFingerprint_ReplaysStoredResult()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var first = CreateInput("operation-1", Fingerprint, "AB-000001");
        await PersistAsync(context, first, CorrelationId);
        context.ChangeTracker.Clear();
        var replay = CreateInput("operation-1", Fingerprint, "AB-000002");

        var result = await PersistAsync(context, replay, "trace-replay");

        Assert.Equal(ResidentCreationPersistenceOutcome.Replayed, result.Outcome);
        Assert.Equal(first.Resident.Id, result.ResidentId);
        Assert.Equal(first.Account.Id, result.AccountId);
        Assert.Equal(first.Address.Id, result.AddressId);
        Assert.Equal(first.Operation.Id, result.OperationId);
        Assert.Equal(1, await context.UserAccounts.CountAsync());
        Assert.Equal(1, await context.Residents.CountAsync());
        Assert.Equal(1, await context.Addresses.CountAsync());
        Assert.Equal(1, await context.Accounts.CountAsync());
        Assert.Equal(1, await context.ResidentCreationOperations.CountAsync());
        Assert.Equal(1, await context.AuditLogs.CountAsync());
    }

    [PostgreSqlFact]
    public async Task CreateAsync_WithConflictingKeyOrAccountNumber_ReturnsTypedConflict()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var first = CreateInput("operation-1", Fingerprint, "AB-000001");
        await PersistAsync(context, first, CorrelationId);
        context.ChangeTracker.Clear();
        var keyConflict = CreateInput(
            "operation-1",
            new string('A', Fingerprint.Length),
            "AB-000002");
        var accountConflict = CreateInput(
            "operation-2",
            Fingerprint,
            " AB-000001 ");

        var keyResult = await PersistAsync(context, keyConflict, "trace-key-conflict");
        context.ChangeTracker.Clear();
        var accountResult = await PersistAsync(
            context,
            accountConflict,
            "trace-account-conflict");

        Assert.Equal(
            ResidentCreationPersistenceOutcome.IdempotencyConflict,
            keyResult.Outcome);
        Assert.Equal(
            ResidentCreationPersistenceOutcome.AccountNumberAlreadyExists,
            accountResult.Outcome);
        Assert.Equal(1, await context.Residents.CountAsync());
        Assert.Equal(1, await context.AuditLogs.CountAsync());
    }

    [PostgreSqlFact]
    public async Task CreateAsync_ConcurrentSameRequest_CreatesOneResident()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using (var migrationContext = database.CreateContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var first = CreateInput("operation-1", Fingerprint, "AB-000001");
        var second = CreateInput("operation-1", Fingerprint, "AB-000001");

        var results = await Task.WhenAll(
            PersistAsync(firstContext, first, CorrelationId),
            PersistAsync(secondContext, second, "trace-concurrent"));

        Assert.Contains(
            results,
            result => result.Outcome is ResidentCreationPersistenceOutcome.Created);
        Assert.Contains(
            results,
            result => result.Outcome is ResidentCreationPersistenceOutcome.Replayed);
        await using var verificationContext = database.CreateContext();
        Assert.Equal(1, await verificationContext.UserAccounts.CountAsync());
        Assert.Equal(1, await verificationContext.Residents.CountAsync());
        Assert.Equal(1, await verificationContext.Addresses.CountAsync());
        Assert.Equal(1, await verificationContext.Accounts.CountAsync());
        Assert.Equal(
            1,
            await verificationContext.ResidentCreationOperations.CountAsync());
        Assert.Equal(1, await verificationContext.AuditLogs.CountAsync());
    }

    private static Task<ResidentCreationPersistenceResult> PersistAsync(
        EcoBilling.Infrastructure.Persistence.EcoBillingDbContext context,
        CreationInput input,
        string correlationId) =>
        new ResidentCreationRepository(context).CreateAsync(
            input.UserAccount,
            input.Resident,
            input.Address,
            input.Account,
            input.Operation,
            ActorId,
            correlationId,
            CancellationToken.None);

    private static CreationInput CreateInput(
        string idempotencyKey,
        string fingerprint,
        string accountNumber)
    {
        var createdAt = new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);
        var userAccount = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            LoginIdentity.Create(LoginType.AccountNumber, accountNumber).Value,
            "stored-resident-password-hash",
            UserRole.Resident,
            createdAt,
            requiresPasswordChange: false).Value;
        var resident = Resident.Create(
            new ResidentId(Guid.NewGuid()),
            userAccount,
            "Ada Lovelace",
            createdAt).Value;
        var address = Address.Create(
            new AddressId(Guid.NewGuid()),
            "Bishkek",
            "Chuy Avenue",
            "42",
            "2",
            "17").Value;
        var account = Account.Create(
            new AccountId(Guid.NewGuid()),
            resident.Id,
            address.Id,
            accountNumber,
            createdAt).Value;
        var operation = ResidentCreationOperation.Create(
            new ResidentCreationOperationId(Guid.NewGuid()),
            idempotencyKey,
            fingerprint,
            resident.Id,
            account.Id,
            address.Id,
            createdAt).Value;
        return new CreationInput(userAccount, resident, address, account, operation);
    }

    private sealed record CreationInput(
        UserAccount UserAccount,
        Resident Resident,
        Address Address,
        Account Account,
        ResidentCreationOperation Operation);
}
