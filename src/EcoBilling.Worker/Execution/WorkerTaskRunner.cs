using System.Diagnostics;
using System.Diagnostics.Metrics;
using EcoBilling.Infrastructure.Observability;
using Microsoft.Extensions.Options;

namespace EcoBilling.Worker.Execution;

public sealed class WorkerTaskRunner
{
    private static readonly ActivitySource ActivitySource = new(
        EcoBillingTelemetry.WorkerActivitySourceName);
    private static readonly Meter Meter = new(EcoBillingTelemetry.WorkerMeterName);
    private static readonly Counter<long> AttemptCounter = Meter.CreateCounter<long>(
        "ecobilling.worker.task.attempts");
    private static readonly Counter<long> CompletionCounter = Meter.CreateCounter<long>(
        "ecobilling.worker.task.completions");
    private static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>(
        "ecobilling.worker.task.retries");
    private static readonly Histogram<double> DurationHistogram = Meter.CreateHistogram<double>(
        "ecobilling.worker.task.duration",
        "ms");

    private readonly WorkerTaskExecutionOptions options;
    private readonly ILogger<WorkerTaskRunner> logger;
    private readonly TimeProvider timeProvider;

    public WorkerTaskRunner(
        IOptions<WorkerTaskExecutionOptions> options,
        ILogger<WorkerTaskRunner> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (options.Value.MaxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Worker task MaxAttempts must be at least one.");
        }

        if (options.Value.RetryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Worker task RetryDelay cannot be negative.");
        }

        this.options = options.Value;
        this.logger = logger;
        this.timeProvider = timeProvider;
    }

    public async Task RunAsync(
        IWorkerTask task,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentException.ThrowIfNullOrWhiteSpace(task.Name);

        var startedAt = timeProvider.GetTimestamp();
        var taskNameTag = new KeyValuePair<string, object?>("worker.task.name", task.Name);
        using var activity = ActivitySource.StartActivity(
            "worker.task.run",
            ActivityKind.Internal);
        activity?.SetTag("worker.task.name", task.Name);

        try
        {
            for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AttemptCounter.Add(1, taskNameTag);
                activity?.AddEvent(
                    new ActivityEvent(
                        "worker.task.attempt",
                        tags: new ActivityTagsCollection
                        {
                            ["worker.task.attempt"] = attempt
                        }));

                try
                {
                    logger.LogDebug(
                        "Executing worker task {WorkerTaskName}, attempt {WorkerTaskAttempt} of {WorkerTaskMaxAttempts}",
                        task.Name,
                        attempt,
                        options.MaxAttempts);

                    await task.ExecuteAsync(cancellationToken);

                    var elapsed = timeProvider.GetElapsedTime(startedAt);
                    CompletionCounter.Add(
                        1,
                        taskNameTag,
                        new KeyValuePair<string, object?>("worker.task.outcome", "success"));
                    DurationHistogram.Record(elapsed.TotalMilliseconds, taskNameTag);
                    activity?.SetTag("worker.task.attempt_count", attempt);
                    activity?.SetTag("worker.task.outcome", "success");
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    logger.LogInformation(
                        "Worker task {WorkerTaskName} completed in {WorkerTaskElapsedMilliseconds} ms after {WorkerTaskAttemptCount} attempt(s)",
                        task.Name,
                        elapsed.TotalMilliseconds,
                        attempt);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (attempt < options.MaxAttempts)
                {
                    RetryCounter.Add(1, taskNameTag);
                    activity?.AddEvent(
                        new ActivityEvent(
                            "worker.task.retry",
                            tags: new ActivityTagsCollection
                            {
                                ["worker.task.attempt"] = attempt,
                                ["error.type"] = exception.GetType().FullName
                            }));
                    logger.LogWarning(
                        exception,
                        "Worker task {WorkerTaskName} failed on attempt {WorkerTaskAttempt}; retrying after {WorkerTaskRetryDelay}",
                        task.Name,
                        attempt,
                        options.RetryDelay);

                    await Task.Delay(options.RetryDelay, timeProvider, cancellationToken);
                }
                catch (Exception exception)
                {
                    var elapsed = timeProvider.GetElapsedTime(startedAt);
                    CompletionCounter.Add(
                        1,
                        taskNameTag,
                        new KeyValuePair<string, object?>("worker.task.outcome", "failure"));
                    DurationHistogram.Record(elapsed.TotalMilliseconds, taskNameTag);
                    activity?.SetTag("worker.task.attempt_count", attempt);
                    activity?.SetTag("worker.task.outcome", "failure");
                    activity?.SetTag("error.type", exception.GetType().FullName);
                    activity?.SetStatus(ActivityStatusCode.Error);
                    logger.LogError(
                        exception,
                        "Worker task {WorkerTaskName} failed after {WorkerTaskAttemptCount} attempt(s)",
                        task.Name,
                        attempt);
                    throw;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var elapsed = timeProvider.GetElapsedTime(startedAt);
            CompletionCounter.Add(
                1,
                taskNameTag,
                new KeyValuePair<string, object?>("worker.task.outcome", "canceled"));
            DurationHistogram.Record(elapsed.TotalMilliseconds, taskNameTag);
            activity?.SetTag("worker.task.outcome", "canceled");
            activity?.SetStatus(ActivityStatusCode.Unset);
            logger.LogInformation(
                "Worker task {WorkerTaskName} was canceled after {WorkerTaskElapsedMilliseconds} ms",
                task.Name,
                elapsed.TotalMilliseconds);
            throw;
        }
    }
}
