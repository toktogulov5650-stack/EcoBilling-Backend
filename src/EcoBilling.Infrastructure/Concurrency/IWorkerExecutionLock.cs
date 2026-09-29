namespace EcoBilling.Infrastructure.Concurrency;

public interface IWorkerExecutionLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        string lockName,
        CancellationToken cancellationToken);
}
