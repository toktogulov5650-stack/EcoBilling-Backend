using EcoBilling.Infrastructure.Outbox;
using Microsoft.Extensions.Options;

namespace EcoBilling.Worker.Execution;

public sealed class OutboxWorkerTask(
    IOutboxDispatcher dispatcher,
    IOptions<WorkerScheduleOptions> options,
    ILogger<OutboxWorkerTask> logger)
    : IWorkerTask
{
    public string Name => "outbox";

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var result = await dispatcher.DispatchBatchAsync(
            options.Value.OutboxBatchSize,
            cancellationToken);

        logger.LogInformation(
            "Outbox batch completed: selected {SelectedCount}, processed {ProcessedCount}, failed {FailedCount}",
            result.Selected,
            result.Processed,
            result.Failed);
    }
}
