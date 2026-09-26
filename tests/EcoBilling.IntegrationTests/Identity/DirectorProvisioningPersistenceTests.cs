using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.IntegrationTests.Identity;

public sealed class DirectorProvisioningPersistenceTests
{
    private const string Fingerprint =
        "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [PostgreSqlFact]
    public async Task ProvisionAsync_CreatesDirectorAccountProfileAndOperationAtomically()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var input = CreateInput("operation-1", Fingerprint, "director@example.com");
        var repository = new DirectorProvisioningRepository(context);

        var result = await repository.ProvisionAsync(
            input.UserAccount,
            input.Director,
            input.Operation,
            CancellationToken.None);

        Assert.Equal(DirectorProvisioningPersistenceOutcome.Created, result.Outcome);
        var storedAccount = await context.UserAccounts.AsNoTracking().SingleAsync();
        var storedDirector = await context.Directors.AsNoTracking().SingleAsync();
        var storedOperation = await context.DirectorProvisioningOperations
            .AsNoTracking()
            .SingleAsync();
        Assert.True(storedAccount.RequiresPasswordChange);
        Assert.Equal(input.UserAccount.PasswordHash, storedAccount.PasswordHash);
        Assert.Equal(input.Director.Id, storedDirector.Id);
        Assert.Equal(input.Operation.Id, storedOperation.Id);
        Assert.Equal(input.Director.Id, storedOperation.DirectorId);
    }

    [PostgreSqlFact]
    public async Task ProvisionAsync_WithSameKeyAndFingerprint_ReturnsStoredResultWithoutDuplicate()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var repository = new DirectorProvisioningRepository(context);
        var first = CreateInput("operation-1", Fingerprint, "director@example.com");
        await repository.ProvisionAsync(
            first.UserAccount,
            first.Director,
            first.Operation,
            CancellationToken.None);
        context.ChangeTracker.Clear();
        var replay = CreateInput("operation-1", Fingerprint, "another@example.com");

        var result = await repository.ProvisionAsync(
            replay.UserAccount,
            replay.Director,
            replay.Operation,
            CancellationToken.None);

        Assert.Equal(DirectorProvisioningPersistenceOutcome.Replayed, result.Outcome);
        Assert.Equal(first.Director.Id, result.DirectorId);
        Assert.Equal(first.Operation.Id, result.OperationId);
        Assert.Equal(1, await context.UserAccounts.CountAsync());
        Assert.Equal(1, await context.Directors.CountAsync());
        Assert.Equal(1, await context.DirectorProvisioningOperations.CountAsync());
    }

    [PostgreSqlFact]
    public async Task ProvisionAsync_WithSameKeyAndDifferentFingerprint_ReturnsConflict()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var repository = new DirectorProvisioningRepository(context);
        var first = CreateInput("operation-1", Fingerprint, "director@example.com");
        await repository.ProvisionAsync(
            first.UserAccount,
            first.Director,
            first.Operation,
            CancellationToken.None);
        context.ChangeTracker.Clear();
        var differentFingerprint = new string('A', Fingerprint.Length);
        var conflicting = CreateInput(
            "operation-1",
            differentFingerprint,
            "another@example.com");

        var result = await repository.ProvisionAsync(
            conflicting.UserAccount,
            conflicting.Director,
            conflicting.Operation,
            CancellationToken.None);

        Assert.Equal(
            DirectorProvisioningPersistenceOutcome.IdempotencyConflict,
            result.Outcome);
        Assert.Equal(1, await context.Directors.CountAsync());
    }

    [PostgreSqlFact]
    public async Task ProvisionAsync_WithAnotherKeyAfterDirectorExists_ReturnsConflict()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var repository = new DirectorProvisioningRepository(context);
        var first = CreateInput("operation-1", Fingerprint, "director@example.com");
        await repository.ProvisionAsync(
            first.UserAccount,
            first.Director,
            first.Operation,
            CancellationToken.None);
        context.ChangeTracker.Clear();
        var second = CreateInput("operation-2", Fingerprint, "another@example.com");

        var result = await repository.ProvisionAsync(
            second.UserAccount,
            second.Director,
            second.Operation,
            CancellationToken.None);

        Assert.Equal(
            DirectorProvisioningPersistenceOutcome.DirectorAlreadyExists,
            result.Outcome);
        Assert.Equal(1, await context.Directors.CountAsync());
    }

    [PostgreSqlFact]
    public async Task ProvisionAsync_WhenDirectorAccountExistsWithoutProfile_ReturnsConflict()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var existingDirector = CreateDirectorAccount("existing@example.com");
        context.UserAccounts.Add(existingDirector);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var input = CreateInput("operation-1", Fingerprint, "another@example.com");

        var result = await new DirectorProvisioningRepository(context).ProvisionAsync(
            input.UserAccount,
            input.Director,
            input.Operation,
            CancellationToken.None);

        Assert.Equal(
            DirectorProvisioningPersistenceOutcome.DirectorAlreadyExists,
            result.Outcome);
        Assert.Equal(1, await context.UserAccounts.CountAsync());
        Assert.Empty(await context.Directors.ToListAsync());
        Assert.Empty(await context.DirectorProvisioningOperations.ToListAsync());
    }

    [PostgreSqlFact]
    public async Task Database_RejectsSecondDirectorAccountWithoutProfiles()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        context.UserAccounts.Add(CreateDirectorAccount("first@example.com"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        context.UserAccounts.Add(CreateDirectorAccount("second@example.com"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task ProvisionAsync_ConcurrentSameOperation_CreatesOneDirector()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using (var migrationContext = database.CreateContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var first = CreateInput("operation-1", Fingerprint, "director@example.com");
        var second = CreateInput("operation-1", Fingerprint, "director@example.com");

        var results = await Task.WhenAll(
            new DirectorProvisioningRepository(firstContext).ProvisionAsync(
                first.UserAccount,
                first.Director,
                first.Operation,
                CancellationToken.None),
            new DirectorProvisioningRepository(secondContext).ProvisionAsync(
                second.UserAccount,
                second.Director,
                second.Operation,
                CancellationToken.None));

        Assert.Contains(
            results,
            result => result.Outcome is DirectorProvisioningPersistenceOutcome.Created);
        Assert.Contains(
            results,
            result => result.Outcome is DirectorProvisioningPersistenceOutcome.Replayed);
        await using var verificationContext = database.CreateContext();
        Assert.Equal(1, await verificationContext.Directors.CountAsync());
        Assert.Equal(1, await verificationContext.UserAccounts.CountAsync());
        Assert.Equal(1, await verificationContext.DirectorProvisioningOperations.CountAsync());
    }

    [PostgreSqlFact]
    public async Task TokenReplayStore_AcceptsTokenOnceAndPurgesExpiredEntries()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using (var migrationContext = database.CreateContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        var now = new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);
        await using (var firstContext = database.CreateContext())
        {
            var accepted = await new InternalServiceTokenReplayStore(firstContext)
                .TryConsumeAsync(
                    "ecobilling-control",
                    "token-1",
                    now.AddMinutes(2),
                    now,
                    CancellationToken.None);
            Assert.True(accepted);
        }

        await using (var replayContext = database.CreateContext())
        {
            var accepted = await new InternalServiceTokenReplayStore(replayContext)
                .TryConsumeAsync(
                    "ecobilling-control",
                    "token-1",
                    now.AddMinutes(2),
                    now,
                    CancellationToken.None);
            Assert.False(accepted);
        }

        await using (var afterExpiryContext = database.CreateContext())
        {
            var accepted = await new InternalServiceTokenReplayStore(afterExpiryContext)
                .TryConsumeAsync(
                    "ecobilling-control",
                    "token-1",
                    now.AddMinutes(5),
                    now.AddMinutes(3),
                    CancellationToken.None);
            Assert.True(accepted);
        }
    }

    private static ProvisioningInput CreateInput(
        string idempotencyKey,
        string fingerprint,
        string email)
    {
        var createdAt = new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);
        var userAccount = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            LoginIdentity.Create(LoginType.Email, email).Value,
            "stored-initial-credential-hash",
            UserRole.Director,
            createdAt,
            requiresPasswordChange: true).Value;
        var director = DirectorProfile.Create(
            new DirectorId(Guid.NewGuid()),
            userAccount,
            "Ada Lovelace",
            createdAt).Value;
        var operation = DirectorProvisioningOperation.Create(
            new DirectorProvisioningOperationId(Guid.NewGuid()),
            idempotencyKey,
            fingerprint,
            director.Id,
            createdAt).Value;
        return new ProvisioningInput(userAccount, director, operation);
    }

    private static UserAccount CreateDirectorAccount(string email) =>
        UserAccount.Create(
            new UserId(Guid.NewGuid()),
            LoginIdentity.Create(LoginType.Email, email).Value,
            "stored-initial-credential-hash",
            UserRole.Director,
            new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.Zero),
            requiresPasswordChange: true).Value;

    private sealed record ProvisioningInput(
        UserAccount UserAccount,
        DirectorProfile Director,
        DirectorProvisioningOperation Operation);
}
