using EcoBilling.Infrastructure.Concurrency;
using Microsoft.Extensions.Options;

namespace EcoBilling.Worker.Execution;

public sealed class ScheduledWorkerService(
    IServiceScopeFactory scopeFactory,
    IWorkerExecutionLock executionLock,
    IOptions<WorkerScheduleOptions> options,
    TimeProvider timeProvider,
    ILogger<ScheduledWorkerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var tasks = new List<Task>();

        if (settings.MonthlyBillingEnabled)
        {
            tasks.Add(
                RunLoopAsync<MonthlyBillingWorkerTask>(
                    settings.MonthlyBillingInterval,
                    stoppingToken));
        }

        if (settings.OutboxEnabled)
        {
            tasks.Add(
                RunLoopAsync<OutboxWorkerTask>(
                    settings.OutboxInterval,
                    stoppingToken));
        }

        if (tasks.Count == 0)
        {
            logger.LogInformation(
                "No EcoBilling worker schedules are enabled; worker remains idle.");
            await Task.Delay(Timeout.InfiniteTimeSpan, timeProvider, stoppingToken);
            return;
        }

        await Task.WhenAll(tasks);
    }

    private async Task RunLoopAsync<TTask>(
        TimeSpan interval,
        CancellationToken cancellationToken)
        where TTask : class, IWorkerTask
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"Worker interval for {typeof(TTask).Name} must be positive.");
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<WorkerTaskRunner>();
            var task = scope.ServiceProvider.GetRequiredService<TTask>();

            await using var lease = await executionLock.TryAcquireAsync(
                task.Name,
                cancellationToken);

            if (lease is null)
            {
                logger.LogDebug(
                    "Worker task {WorkerTaskName} skipped because another worker instance owns the distributed lock",
                    task.Name);
            }
            else
            {
                await runner.RunAsync(task, cancellationToken);
            }

            await Task.Delay(interval, timeProvider, cancellationToken);
        }
    }
}
