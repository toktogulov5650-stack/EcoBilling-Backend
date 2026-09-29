namespace EcoBilling.Worker.Execution;

public sealed class WorkerScheduleOptions
{
    public const string SectionName = "Worker:Schedules";

    public bool MonthlyBillingEnabled { get; init; }

    public TimeSpan MonthlyBillingInterval { get; init; } = TimeSpan.FromHours(24);

    public bool OutboxEnabled { get; init; }

    public TimeSpan OutboxInterval { get; init; } = TimeSpan.FromMinutes(1);

    public int OutboxBatchSize { get; init; } = 100;
}
