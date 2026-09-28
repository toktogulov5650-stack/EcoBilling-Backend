using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.IntegrationTests.Controllers;

public sealed class ControllerCreationPersistenceTests
{
    private const string Fingerprint =
        "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
    private const string ActorId = "director-user-id";
    private const string CorrelationId = "trace-controller-creation";

    [PostgreSqlFact]
    public async Task CreateAsync_CreatesAccountControllerOperationAndAuditAtomically()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var input = CreateInput("operation-1", Fingerprint, "agent@example.com");

        var result = await new ControllerCreationRepository(context).CreateAsync(
            input.UserAccount,
            input.Controller,
            input.Operation,
            ActorId,
            CorrelationId,
            CancellationToken.None);

        Assert.Equal(ControllerCreationPersistenceOutcome.Created, result.Outcome);
        var storedAccount = await context.UserAccounts.AsNoTracking().SingleAsync();
        var storedController = await context.Controllers.AsNoTracking().SingleAsync();
        var storedOperation = await context.ControllerCreationOperations
            .AsNoTracking()
            .SingleAsync();
        var storedAudit = await context.AuditLogs.AsNoTracking().SingleAsync();
        Assert.True(storedAccount.RequiresPasswordChange);
        Assert.Equal(UserRole.Controller, storedAccount.Role);
        Assert.Equal(input.Controller.Id, storedController.Id);
        Assert.Equal(input.Operation.Id, storedOperation.Id);
        Assert.Equal(input.Controller.Id, storedOperation.ControllerId);
        Assert.Equal("User", storedAudit.ActorType);
        Assert.Equal(ActorId, storedAudit.ActorId);
        Assert.Equal("controllers.controller.created", storedAudit.Action);
        Assert.Equal("Controller", storedAudit.EntityType);
        Assert.Equal(input.Controller.Id.Value.ToString("D"), storedAudit.EntityId);
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
            "agent@example.com",
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
        var repository = new ControllerCreationRepository(context);
        var first = CreateInput("operation-1", Fingerprint, "first@example.com");
        await repository.CreateAsync(
            first.UserAccount,
            first.Controller,
            first.Operation,
            ActorId,
            CorrelationId,
            CancellationToken.None);
        context.ChangeTracker.Clear();
        var replay = CreateInput("operation-1", Fingerprint, "second@example.com");

        var result = await repository.CreateAsync(
            replay.UserAccount,
            replay.Controller,
            replay.Operation,
            ActorId,
            "trace-replay",
            CancellationToken.None);

        Assert.Equal(ControllerCreationPersistenceOutcome.Replayed, result.Outcome);
        Assert.Equal(first.Controller.Id, result.ControllerId);
        Assert.Equal(first.Operation.Id, result.OperationId);
        Assert.Equal(1, await context.UserAccounts.CountAsync());
        Assert.Equal(1, await context.Controllers.CountAsync());
        Assert.Equal(1, await context.ControllerCreationOperations.CountAsync());
        Assert.Equal(1, await context.AuditLogs.CountAsync());
    }

    [PostgreSqlFact]
    public async Task CreateAsync_WithConflictingKeyOrEmail_ReturnsTypedConflict()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var repository = new ControllerCreationRepository(context);
        var first = CreateInput("operation-1", Fingerprint, "first@example.com");
        await repository.CreateAsync(
            first.UserAccount,
            first.Controller,
            first.Operation,
            ActorId,
            CorrelationId,
            CancellationToken.None);
        context.ChangeTracker.Clear();
        var keyConflict = CreateInput(
            "operation-1",
            new string('A', Fingerprint.Length),
            "second@example.com");
        var emailConflict = CreateInput(
            "operation-2",
            Fingerprint,
            "first@example.com");

        var keyResult = await repository.CreateAsync(
            keyConflict.UserAccount,
            keyConflict.Controller,
            keyConflict.Operation,
            ActorId,
            "trace-key-conflict",
            CancellationToken.None);
        context.ChangeTracker.Clear();
        var emailResult = await repository.CreateAsync(
            emailConflict.UserAccount,
            emailConflict.Controller,
            emailConflict.Operation,
            ActorId,
            "trace-email-conflict",
            CancellationToken.None);

        Assert.Equal(
            ControllerCreationPersistenceOutcome.IdempotencyConflict,
            keyResult.Outcome);
        Assert.Equal(
            ControllerCreationPersistenceOutcome.EmailAlreadyExists,
            emailResult.Outcome);
        Assert.Equal(1, await context.Controllers.CountAsync());
        Assert.Equal(1, await context.AuditLogs.CountAsync());
    }

    [PostgreSqlFact]
    public async Task CreateAsync_ConcurrentSameRequest_CreatesOneController()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using (var migrationContext = database.CreateContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var first = CreateInput("operation-1", Fingerprint, "agent@example.com");
        var second = CreateInput("operation-1", Fingerprint, "agent@example.com");

        var results = await Task.WhenAll(
            new ControllerCreationRepository(firstContext).CreateAsync(
                first.UserAccount,
                first.Controller,
                first.Operation,
                ActorId,
                CorrelationId,
                CancellationToken.None),
            new ControllerCreationRepository(secondContext).CreateAsync(
                second.UserAccount,
                second.Controller,
                second.Operation,
                ActorId,
                "trace-concurrent",
                CancellationToken.None));

        Assert.Contains(
            results,
            result => result.Outcome is ControllerCreationPersistenceOutcome.Created);
        Assert.Contains(
            results,
            result => result.Outcome is ControllerCreationPersistenceOutcome.Replayed);
        await using var verificationContext = database.CreateContext();
        Assert.Equal(1, await verificationContext.Controllers.CountAsync());
        Assert.Equal(1, await verificationContext.UserAccounts.CountAsync());
        Assert.Equal(
            1,
            await verificationContext.ControllerCreationOperations.CountAsync());
        Assert.Equal(1, await verificationContext.AuditLogs.CountAsync());
    }

    private static CreationInput CreateInput(
        string idempotencyKey,
        string fingerprint,
        string email)
    {
        var createdAt = new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);
        var userAccount = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            LoginIdentity.Create(LoginType.Email, email).Value,
            "stored-initial-credential-hash",
            UserRole.Controller,
            createdAt,
            requiresPasswordChange: true).Value;
        var controller = Controller.Create(
            new ControllerId(Guid.NewGuid()),
            userAccount,
            "Grace Hopper",
            createdAt).Value;
        var operation = ControllerCreationOperation.Create(
            new ControllerCreationOperationId(Guid.NewGuid()),
            idempotencyKey,
            fingerprint,
            controller.Id,
            createdAt).Value;
        return new CreationInput(userAccount, controller, operation);
    }

    private sealed record CreationInput(
        UserAccount UserAccount,
        Controller Controller,
        ControllerCreationOperation Operation);
}
