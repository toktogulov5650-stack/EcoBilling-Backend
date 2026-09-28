using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.IntegrationTests.Identity;

public sealed class RefreshSessionPersistenceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 28, 8, 0, 0, TimeSpan.Zero);

    [PostgreSqlFact]
    public async Task Rotate_ConsumesCurrentAndCreatesReplacementInSameFamily()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using (var context = database.CreateContext())
        {
            await context.Database.MigrateAsync();
            var user = CreateUser();
            context.UserAccounts.Add(user);
            await context.SaveChangesAsync();

            var repository = new RefreshSessionRepository(context);
            var current = CreateSession(user.Id, new string('A', 64));
            await repository.CreateAsync(current, CancellationToken.None);

            var replacementId = new RefreshSessionId(Guid.NewGuid());
            var result = await repository.RotateAsync(
                current.TokenHash,
                replacementId,
                new string('B', 64),
                Now.AddMinutes(1),
                Now.AddDays(30),
                CancellationToken.None);

            Assert.Equal(RefreshSessionRotationOutcome.Rotated, result.Outcome);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(user.Role, result.Role);
        }

        await using var verificationContext = database.CreateContext();
        var sessions = await verificationContext.RefreshSessions
            .OrderBy(session => session.CreatedAt)
            .ToArrayAsync();
        Assert.Equal(2, sessions.Length);
        Assert.Equal(Now.AddMinutes(1), sessions[0].ConsumedAt);
        Assert.Equal(sessions[1].Id, sessions[0].ReplacedBySessionId);
        Assert.Equal(sessions[0].UserId, sessions[1].UserId);
        Assert.Equal(sessions[0].FamilyId, sessions[1].FamilyId);
        Assert.Null(sessions[1].RevokedAt);
    }

    [PostgreSqlFact]
    public async Task Rotate_ReusedTokenRevokesEntireFamily()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var user = CreateUser();
        context.UserAccounts.Add(user);
        await context.SaveChangesAsync();
        var repository = new RefreshSessionRepository(context);
        var current = CreateSession(user.Id, new string('C', 64));
        await repository.CreateAsync(current, CancellationToken.None);
        await repository.RotateAsync(
            current.TokenHash,
            new RefreshSessionId(Guid.NewGuid()),
            new string('D', 64),
            Now.AddMinutes(1),
            Now.AddDays(30),
            CancellationToken.None);

        var reuse = await repository.RotateAsync(
            current.TokenHash,
            new RefreshSessionId(Guid.NewGuid()),
            new string('E', 64),
            Now.AddMinutes(2),
            Now.AddDays(30),
            CancellationToken.None);

        Assert.Equal(RefreshSessionRotationOutcome.Reused, reuse.Outcome);
        Assert.All(
            await context.RefreshSessions.AsNoTracking().ToArrayAsync(),
            session => Assert.Equal(Now.AddMinutes(2), session.RevokedAt));
    }

    [PostgreSqlFact]
    public async Task Revoke_RevokesFamilyAndIsIdempotentForUnknownToken()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var user = CreateUser();
        context.UserAccounts.Add(user);
        await context.SaveChangesAsync();
        var repository = new RefreshSessionRepository(context);
        var session = CreateSession(user.Id, new string('F', 64));
        await repository.CreateAsync(session, CancellationToken.None);

        await repository.RevokeAsync(session.TokenHash, Now.AddMinutes(1), CancellationToken.None);
        await repository.RevokeAsync(new string('0', 64), Now.AddMinutes(2), CancellationToken.None);

        var persisted = await context.RefreshSessions.AsNoTracking().SingleAsync();
        Assert.Equal(Now.AddMinutes(1), persisted.RevokedAt);
    }

    private static UserAccount CreateUser() =>
        UserAccount.Create(
            new UserId(Guid.NewGuid()),
            LoginIdentity.Create(LoginType.Email, "controller@example.com").Value,
            "stored-password-hash",
            UserRole.Controller,
            Now).Value;

    private static RefreshSession CreateSession(UserId userId, string tokenHash) =>
        RefreshSession.Create(
            new RefreshSessionId(Guid.NewGuid()),
            userId,
            Guid.NewGuid(),
            tokenHash,
            Now,
            Now.AddDays(30)).Value;
}
