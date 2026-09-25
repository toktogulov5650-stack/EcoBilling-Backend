using EcoBilling.Infrastructure;
using EcoBilling.Worker.Execution;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("EcoBilling");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'EcoBilling' is required. Configure it without committing secrets, for example through ConnectionStrings__EcoBilling.");
}

builder.Services.AddEcoBillingInfrastructure(connectionString);
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
builder.Services.AddSingleton<WorkerTaskRunner>();

var host = builder.Build();
host.Run();

public partial class Program;
