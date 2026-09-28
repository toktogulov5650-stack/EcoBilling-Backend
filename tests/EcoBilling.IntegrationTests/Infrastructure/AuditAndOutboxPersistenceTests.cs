using System.Text.Json;
using EcoBilling.Infrastructure.Auditing;
using EcoBilling.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.IntegrationTests.Infrastructure;

public sealed class AuditAndOutboxPersistenceTests
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AuditLog_RejectsInvalidJson()
    {
        Assert.Throws<ArgumentException>(
            () => new AuditLog(
                Guid.NewGuid(),
                "InternalService",
                "ecobilling-control",
                "identity.director.provisioned",
                "Director",
                Guid.NewGuid().ToString("D"),
                beforeData: null,
                afterData: "not-json",
                "trace-id",
                OccurredAt));
    }

    [Fact]
    public void OutboxMessage_EnforcesStateTransitions()
    {
        var message = new OutboxMessage(
            Guid.NewGuid(),
            "example.event.v1",
            """{"id":"value"}""",
            OccurredAt);

        message.RecordFailure("temporary failure");
        message.MarkProcessed(OccurredAt.AddMinutes(1));

        Assert.Equal(1, message.RetryCount);
        Assert.Null(message.LastError);
        Assert.Equal(OccurredAt.AddMinutes(1), message.ProcessedAt);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OutboxMessage(
                    Guid.NewGuid(),
                    "example.event.v1",
                    "{}",
                    OccurredAt)
                .MarkProcessed(OccurredAt.AddTicks(-1)));
        Assert.Throws<InvalidOperationException>(
            () => message.RecordFailure("late failure"));
        Assert.Throws<InvalidOperationException>(
            () => message.MarkProcessed(OccurredAt.AddMinutes(2)));
    }

    [PostgreSqlFact]
    public async Task Migration_PersistsAuditAndOutboxWithJsonbAndUtcTimestamps()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var audit = CreateAudit();
        var message = new OutboxMessage(
            Guid.NewGuid(),
            "example.event.v1",
            """{"entityId":"42"}""",
            OccurredAt);
        context.AuditLogs.Add(audit);
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var storedAudit = await context.AuditLogs.AsNoTracking().SingleAsync();
        var storedMessage = await context.OutboxMessages.SingleAsync();

        Assert.Equal(audit.Id, storedAudit.Id);
        AssertJsonEquivalent("""{"entityId":"42"}""", storedAudit.AfterData);
        Assert.Equal(OccurredAt, storedAudit.CreatedAt);
        Assert.Equal(message.Id, storedMessage.Id);
        AssertJsonEquivalent("""{"entityId":"42"}""", storedMessage.Payload);
        Assert.Equal(OccurredAt, storedMessage.OccurredAt);
        Assert.Null(storedMessage.ProcessedAt);
        Assert.Equal(0, storedMessage.RetryCount);
        Assert.Null(storedMessage.LastError);

        storedMessage.RecordFailure("temporary failure");
        await context.SaveChangesAsync();
        storedMessage.MarkProcessed(OccurredAt.AddMinutes(1));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var processedMessage = await context.OutboxMessages
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(1, processedMessage.RetryCount);
        Assert.Equal(OccurredAt.AddMinutes(1), processedMessage.ProcessedAt);
        Assert.Null(processedMessage.LastError);
    }

    [PostgreSqlFact]
    public async Task AuditLog_IsAppendOnlyThroughDbContext()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        var audit = CreateAudit();
        context.AuditLogs.Add(audit);
        await context.SaveChangesAsync();

        context.AuditLogs.Remove(audit);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
        Assert.Contains("append-only", exception.Message, StringComparison.Ordinal);
        context.Entry(audit).State = EntityState.Unchanged;
        Assert.Equal(1, await context.AuditLogs.CountAsync());
    }

    private static AuditLog CreateAudit() =>
        new(
            Guid.NewGuid(),
            "InternalService",
            "ecobilling-control",
            "identity.director.provisioned",
            "Director",
            Guid.NewGuid().ToString("D"),
            beforeData: null,
            afterData: """{"entityId":"42"}""",
            "trace-id",
            OccurredAt);

    private static void AssertJsonEquivalent(string expected, string? actual)
    {
        Assert.NotNull(actual);
        using var expectedDocument = JsonDocument.Parse(expected);
        using var actualDocument = JsonDocument.Parse(actual);
        Assert.True(
            JsonElement.DeepEquals(
                expectedDocument.RootElement,
                actualDocument.RootElement));
    }
}
