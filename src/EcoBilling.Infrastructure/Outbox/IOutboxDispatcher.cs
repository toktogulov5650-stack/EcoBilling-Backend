namespace EcoBilling.Infrastructure.Outbox;

public interface IOutboxDispatcher
{
    Task<OutboxDispatchResult> DispatchBatchAsync(
        int batchSize,
        CancellationToken cancellationToken);
}

public sealed record OutboxDispatchResult(
    int Selected,
    int Processed,
    int Failed);
