using EcoBilling.Worker.Execution;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EcoBilling.UnitTests.Worker.Execution;

public sealed class WorkerTaskRunnerTests
{
    [Fact]
    public async Task RunAsync_WhenTaskSucceeds_ExecutesOnce()
    {
        var task = new RecordingWorkerTask((_, _) => Task.CompletedTask);
        var runner = CreateRunner(maxAttempts: 3);

        await runner.RunAsync(task, CancellationToken.None);

        Assert.Equal(1, task.AttemptCount);
    }

    [Fact]
    public async Task RunAsync_WhenTransientFailuresOccur_RetriesUntilSuccess()
    {
        var task = new RecordingWorkerTask(
            (attempt, _) => attempt < 3
                ? Task.FromException(new InvalidOperationException("Transient failure."))
                : Task.CompletedTask);
        var runner = CreateRunner(maxAttempts: 3);

        await runner.RunAsync(task, CancellationToken.None);

        Assert.Equal(3, task.AttemptCount);
    }

    [Fact]
    public async Task RunAsync_WhenAttemptsAreExhausted_PropagatesLastException()
    {
        var expectedException = new InvalidOperationException("Persistent failure.");
        var task = new RecordingWorkerTask(
            (_, _) => Task.FromException(expectedException));
        var runner = CreateRunner(maxAttempts: 3);

        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => runner.RunAsync(task, CancellationToken.None));

        Assert.Same(expectedException, actualException);
        Assert.Equal(3, task.AttemptCount);
    }

    [Fact]
    public async Task RunAsync_WhenCancellationIsRequested_DoesNotRetry()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var task = new RecordingWorkerTask(
            (_, cancellationToken) =>
            {
                cancellationTokenSource.Cancel();
                return Task.FromCanceled(cancellationToken);
            });
        var runner = CreateRunner(maxAttempts: 3);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runner.RunAsync(task, cancellationTokenSource.Token));

        Assert.Equal(1, task.AttemptCount);
    }

    [Fact]
    public async Task RunAsync_WithAlreadyCanceledToken_DoesNotExecuteTask()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var task = new RecordingWorkerTask((_, _) => Task.CompletedTask);
        var runner = CreateRunner(maxAttempts: 3);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runner.RunAsync(task, cancellationTokenSource.Token));

        Assert.Equal(0, task.AttemptCount);
    }

    [Fact]
    public async Task RunAsync_WhenCanceledDuringRetryDelay_DoesNotStartAnotherAttempt()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var firstAttemptCompleted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var task = new RecordingWorkerTask(
            (_, _) =>
            {
                firstAttemptCompleted.SetResult();
                return Task.FromException(new InvalidOperationException("Transient failure."));
            });
        var runner = CreateRunner(
            maxAttempts: 3,
            retryDelay: TimeSpan.FromHours(1));

        var execution = runner.RunAsync(task, cancellationTokenSource.Token);
        await firstAttemptCompleted.Task;
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        Assert.Equal(1, task.AttemptCount);
    }

    [Fact]
    public void Constructor_WithInvalidMaxAttempts_Throws()
    {
        var options = Options.Create(
            new WorkerTaskExecutionOptions
            {
                MaxAttempts = 0,
                RetryDelay = TimeSpan.Zero
            });

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new WorkerTaskRunner(
                options,
                NullLogger<WorkerTaskRunner>.Instance,
                TimeProvider.System));
    }

    [Fact]
    public void Constructor_WithNegativeRetryDelay_Throws()
    {
        var options = Options.Create(
            new WorkerTaskExecutionOptions
            {
                MaxAttempts = 1,
                RetryDelay = TimeSpan.FromMilliseconds(-1)
            });

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new WorkerTaskRunner(
                options,
                NullLogger<WorkerTaskRunner>.Instance,
                TimeProvider.System));
    }

    private static WorkerTaskRunner CreateRunner(
        int maxAttempts,
        TimeSpan? retryDelay = null) =>
        new(
            Options.Create(
                new WorkerTaskExecutionOptions
                {
                    MaxAttempts = maxAttempts,
                    RetryDelay = retryDelay ?? TimeSpan.Zero
                }),
            NullLogger<WorkerTaskRunner>.Instance,
            TimeProvider.System);

    private sealed class RecordingWorkerTask(
        Func<int, CancellationToken, Task> execute)
        : IWorkerTask
    {
        public string Name => "recording-task";

        public int AttemptCount { get; private set; }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            AttemptCount++;
            return execute(AttemptCount, cancellationToken);
        }
    }
}
