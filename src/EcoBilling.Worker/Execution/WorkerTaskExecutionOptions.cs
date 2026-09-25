namespace EcoBilling.Worker.Execution;

public sealed class WorkerTaskExecutionOptions
{
    public const string SectionName = "Worker:Execution";

    public int MaxAttempts { get; init; } = 3;

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(5);
}
