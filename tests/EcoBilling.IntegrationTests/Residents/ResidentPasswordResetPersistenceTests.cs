using EcoBilling.Infrastructure.Persistence;
using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.IntegrationTests.Residents;

public sealed class ResidentPasswordResetPersistenceTests
{
    private const string Fingerprint =
        "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
    private const string ActorId = "director-user-id";
    private const string CorrelationId = "trace-resident-password-reset";
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    [PostgreSqlFact]
    public async Task ResetAsync_ChangesPasswordClearsLockoutRevokesSessionsAndAudits()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var seed = await SeedResidentWithSessionsAsync(context);
        var operation = CreateOperation(seed.Resident.Id, "reset-1", Fingerprint);

        var result = await new ResidentPasswordResetRepository(context).ResetAsync(
            operation,
            "replacement-password-hash",
            ActorId,
            CorrelationId,
            CancellationToken.None);

        Assert.Equal(ResidentPasswordResetPersistenceOutcome.Reset, result.Outcome);
        Assert.Equal(operation.Id, result.OperationId);
        context.ChangeTracker.Clear();
        var storedIdentity = await context.UserAccounts.AsNoTracking().SingleAsync();
        var storedSessions = await context.RefreshSessions.AsNoTracking().ToArrayAsync();
        var storedOperation = await context.ResidentPasswordResetOperations
            .AsNoTracking()
            .SingleAsync();
        var audit = await context.AuditLogs.AsNoTracking().SingleAsync();
        Assert.Equal("replacement-password-hash", storedIdentity.PasswordHash);
        Assert.Equal(0, storedIdentity.FailedLoginAttempts);
        Assert.Null(storedIdentity.LockoutEnd);
        Assert.All(storedSessions, session => Assert.Equal(CreatedAt, session.RevokedAt));
        Assert.Equal(operation.Id, storedOperation.Id);
        Assert.Equal("residents.resident.password_reset", audit.Action);
        Assert.Equal(seed.Resident.Id.Value.ToString("D"), audit.EntityId);
        Assert.Equal(ActorId, audit.ActorId);
        Assert.Equal(CorrelationId, audit.CorrelationId);
        Assert.DoesNotContain(
            "replacement-password-hash",
            audit.AfterData ?? string.Empty,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "resident-password",
            audit.AfterData ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [PostgreSqlFact]
    public async Task ResetAsync_WithSameKeyAndFingerprint_ReplaysWithoutSecondMutation()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var seed = await SeedResidentWithSessionsAsync(context);
        var first = CreateOperation(seed.Resident.Id, "reset-1", Fingerprint);
        var repository = new ResidentPasswordResetRepository(context);
        await repository.ResetAsync(
            first,
            "first-replacement-hash",
            ActorId,
            CorrelationId,
            CancellationToken.None);
        context.ChangeTracker.Clear();
        var replay = CreateOperation(seed.Resident.Id, "reset-1", Fingerprint);

        var result = await repository.ResetAsync(
            replay,
            "must-not-be-stored",
            ActorId,
            "trace-replay",
            CancellationToken.None);

        Assert.Equal(ResidentPasswordResetPersistenceOutcome.Replayed, result.Outcome);
        Assert.Equal(first.Id, result.OperationId);
        Assert.Equal(
            "first-replacement-hash",
            (await context.UserAccounts.AsNoTracking().SingleAsync()).PasswordHash);
        Assert.Equal(1, await context.ResidentPasswordResetOperations.CountAsync());
        Assert.Equal(1, await context.AuditLogs.CountAsync());
    }

    [PostgreSqlFact]
    public async Task ResetAsync_WithConflictingKeyOrMissingResident_ReturnsTypedOutcome()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var seed = await SeedResidentWithSessionsAsync(context);
        var repository = new ResidentPasswordResetRepository(context);
        await repository.ResetAsync(
            CreateOperation(seed.Resident.Id, "reset-1", Fingerprint),
            "first-replacement-hash",
            ActorId,
            CorrelationId,
            CancellationToken.None);
        context.ChangeTracker.Clear();

        var conflict = await repository.ResetAsync(
            CreateOperation(
                seed.Resident.Id,
                "reset-1",
                new string('A', Fingerprint.Length)),
            "conflicting-hash",
            ActorId,
            "trace-conflict",
            CancellationToken.None);
        var notFound = await repository.ResetAsync(
            CreateOperation(new ResidentId(Guid.NewGuid()), "reset-2", Fingerprint),
            "missing-resident-hash",
            ActorId,
            "trace-not-found",
            CancellationToken.None);

        Assert.Equal(
            ResidentPasswordResetPersistenceOutcome.IdempotencyConflict,
            conflict.Outcome);
        Assert.Equal(
            ResidentPasswordResetPersistenceOutcome.ResidentNotFound,
            notFound.Outcome);
        Assert.Equal(1, await context.ResidentPasswordResetOperations.CountAsync());
        Assert.Equal(1, await context.AuditLogs.CountAsync());
    }

    [PostgreSqlFact]
    public async Task ResetAsync_ConcurrentSameRequest_PerformsOneReset()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        ResidentId residentId;
        await using (var setupContext = database.CreateContext())
        {
            await setupContext.Database.MigrateAsync();
            residentId = (await SeedResidentWithSessionsAsync(setupContext)).Resident.Id;
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var first = CreateOperation(residentId, "reset-1", Fingerprint);
        var second = CreateOperation(residentId, "reset-1", Fingerprint);

        var results = await Task.WhenAll(
            new ResidentPasswordResetRepository(firstContext).ResetAsync(
                first,
                "replacement-password-hash",
                ActorId,
                CorrelationId,
                CancellationToken.None),
            new ResidentPasswordResetRepository(secondContext).ResetAsync(
                second,
                "replacement-password-hash",
                ActorId,
                "trace-concurrent",
                CancellationToken.None));

        Assert.Contains(
            results,
            result => result.Outcome is ResidentPasswordResetPersistenceOutcome.Reset);
        Assert.Contains(
            results,
            result => result.Outcome is ResidentPasswordResetPersistenceOutcome.Replayed);
        await using var verificationContext = database.CreateContext();
        Assert.Equal(
            1,
            await verificationContext.ResidentPasswordResetOperations.CountAsync());
        Assert.Equal(1, await verificationContext.AuditLogs.CountAsync());
    }

    private static async Task<SeedData> SeedResidentWithSessionsAsync(
        EcoBillingDbContext context)
    {
        var userAccount = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            LoginIdentity.Create(LoginType.AccountNumber, "AB-RESET-1").Value,
            "original-password-hash",
            UserRole.Resident,
            CreatedAt.AddDays(-1)).Value;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            userAccount.RecordFailedLogin(
                CreatedAt.AddMinutes(-1),
                5,
                TimeSpan.FromMinutes(15));
        }

        var resident = Resident.Create(
            new ResidentId(Guid.NewGuid()),
            userAccount,
            "Ada Lovelace",
            CreatedAt.AddDays(-1)).Value;
        var firstSession = RefreshSession.Create(
            new RefreshSessionId(Guid.NewGuid()),
            userAccount.Id,
            Guid.NewGuid(),
            new string('A', RefreshSession.TokenHashLength),
            CreatedAt.AddHours(-1),
            CreatedAt.AddDays(1)).Value;
        var secondSession = RefreshSession.Create(
            new RefreshSessionId(Guid.NewGuid()),
            userAccount.Id,
            Guid.NewGuid(),
            new string('B', RefreshSession.TokenHashLength),
            CreatedAt.AddMinutes(-30),
            CreatedAt.AddDays(1)).Value;

        context.UserAccounts.Add(userAccount);
        context.Residents.Add(resident);
        context.RefreshSessions.AddRange(firstSession, secondSession);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new SeedData(userAccount, resident);
    }

    private static ResidentPasswordResetOperation CreateOperation(
        ResidentId residentId,
        string idempotencyKey,
        string fingerprint) =>
        ResidentPasswordResetOperation.Create(
            new ResidentPasswordResetOperationId(Guid.NewGuid()),
            idempotencyKey,
            fingerprint,
            residentId,
            CreatedAt).Value;

    private sealed record SeedData(UserAccount UserAccount, Resident Resident);
}
