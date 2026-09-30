using EcoBilling.Infrastructure.Concurrency;

namespace EcoBilling.Worker.Execution;

public sealed class OneShotWorkerTaskRunner(
    IServiceProvider serviceProvider,
    IWorkerExecutionLock executionLock,
    WorkerTaskRunner runner,
    ILogger<OneShotWorkerTaskRunner> logger)
{
    public async Task RunAsync(
        string taskName,
        CancellationToken cancellationToken)
    {
        IWorkerTask task = taskName switch
        {
            "monthly-billing" => serviceProvider.GetRequiredService<MonthlyBillingWorkerTask>(),
            "outbox" => serviceProvider.GetRequiredService<OutboxWorkerTask>(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(taskName),
                taskName,
                "The worker task is not registered.")
        };

        await using var lease = await executionLock.TryAcquireAsync(
            task.Name,
            cancellationToken);

        if (lease is null)
        {
            logger.LogInformation(
                "Worker task {WorkerTaskName} skipped because another execution owns the distributed lock",
                task.Name);
            return;
        }

        await runner.RunAsync(task, cancellationToken);
    }
}
