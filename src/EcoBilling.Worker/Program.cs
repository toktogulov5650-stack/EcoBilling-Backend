using EcoBilling.Infrastructure;
using EcoBilling.Infrastructure.Observability;
using EcoBilling.Worker.Execution;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddEcoBillingStructuredLogging(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("EcoBilling");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'EcoBilling' is required. Configure it without committing secrets, for example through ConnectionStrings__EcoBilling.");
}

builder.Services.AddEcoBillingInfrastructure(connectionString);
builder.Services.AddEcoBillingObservability(
    builder.Configuration,
    EcoBillingTelemetry.WorkerServiceName,
    instrumentAspNetCore: false);
builder.Services
    .AddOptions<WorkerTaskExecutionOptions>()
    .Bind(builder.Configuration.GetSection(WorkerTaskExecutionOptions.SectionName))
    .Validate(
        options => options.MaxAttempts >= 1,
        "Worker task MaxAttempts must be at least one.")
    .Validate(
        options => options.RetryDelay >= TimeSpan.Zero,
        "Worker task RetryDelay cannot be negative.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services
    .AddOptions<WorkerScheduleOptions>()
    .Bind(builder.Configuration.GetSection(WorkerScheduleOptions.SectionName))
    .Validate(
        options => options.MonthlyBillingInterval > TimeSpan.Zero,
        "Monthly billing interval must be positive.")
    .Validate(
        options => options.OutboxInterval > TimeSpan.Zero,
        "Outbox interval must be positive.")
    .Validate(
        options => options.OutboxBatchSize is >= 1 and <= 1000,
        "Outbox batch size must be between 1 and 1000.")
    .ValidateOnStart();
builder.Services.AddScoped<WorkerTaskRunner>();
builder.Services.AddScoped<MonthlyBillingWorkerTask>();
builder.Services.AddScoped<OutboxWorkerTask>();

var oneShotTaskName = WorkerTaskSelection.GetTaskName(args);
if (oneShotTaskName is null)
{
    builder.Services.AddHostedService<ScheduledWorkerService>();
}
else
{
    builder.Services.AddScoped<OneShotWorkerTaskRunner>();
}

var host = builder.Build();

if (oneShotTaskName is null)
{
    host.Run();
}
else
{
    await host.StartAsync();
    await using var scope = host.Services.CreateAsyncScope();
    var runner = scope.ServiceProvider.GetRequiredService<OneShotWorkerTaskRunner>();
    await runner.RunAsync(oneShotTaskName, CancellationToken.None);
    await host.StopAsync();
}

public partial class Program;
