using EcoBilling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Outbox;

public sealed class OutboxDispatcher(
    EcoBillingDbContext dbContext,
    IEnumerable<IOutboxMessagePublisher> publishers,
    TimeProvider timeProvider)
    : IOutboxDispatcher
{
    private const long DispatcherLockId = 4_288_965_301_774_126_909;

    public async Task<OutboxDispatchResult> DispatchBatchAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (batchSize is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({DispatcherLockId})",
            cancellationToken);

        var selected = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var processed = 0;
        var failed = 0;
        foreach (var message in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var publisher = publishers.FirstOrDefault(
                candidate => candidate.CanPublish(message.Type));
            if (publisher is null)
            {
                message.RecordFailure(
                    $"No outbox publisher is registered for '{message.Type}'.");
                failed++;
                continue;
            }

            try
            {
                await publisher.PublishAsync(
                    message.Type,
                    message.Payload,
                    cancellationToken);
                message.MarkProcessed(timeProvider.GetUtcNow());
                processed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.RecordFailure(exception.GetType().Name);
                failed++;
            }
        }

        if (selected.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return new OutboxDispatchResult(selected.Count, processed, failed);
    }
}
