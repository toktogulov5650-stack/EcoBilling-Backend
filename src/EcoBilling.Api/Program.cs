using System.Diagnostics;
using EcoBilling.Api.Configuration;
using EcoBilling.Api.Endpoints;
using EcoBilling.Api.InternalEndpoints;
using EcoBilling.Api.Middleware;
using EcoBilling.Infrastructure;
using EcoBilling.Infrastructure.Observability;
using EcoBilling.Modules.Controllers.Features.AssignAddress;
using EcoBilling.Modules.Controllers.Features.CreateController;
using EcoBilling.Modules.Controllers.Features.GetMyAssignments;
using EcoBilling.Modules.Controllers.Features.GetProfile;
using EcoBilling.Modules.Controllers.Features.GetWorklist;
using EcoBilling.Modules.Controllers.Features.RemoveAssignment;
using EcoBilling.Modules.Identity.Application.ProvisionDirector;
using EcoBilling.Modules.Meters.Features.Create;
using EcoBilling.Modules.Meters.Features.ListByAccount;
using EcoBilling.Modules.Meters.Features.Replace;
using EcoBilling.Modules.Readings.Features.Add;
using EcoBilling.Modules.Readings.Features.ListByMeter;
using EcoBilling.Modules.Tariffs.Features.AssignToAccount;
using EcoBilling.Modules.Tariffs.Features.Create;
using EcoBilling.Modules.Tariffs.Features.CreateVersion;
using EcoBilling.Modules.Tariffs.Features.List;
using EcoBilling.Modules.Tariffs.Features.ListVersions;
using EcoBilling.Modules.Billing.Features.CalculateMonthly;
using EcoBilling.Modules.Billing.Features.ListByAccount;
using EcoBilling.Modules.Payments.Features.GetFinancialSummary;
using EcoBilling.Modules.Payments.Features.ListByAccount;
using EcoBilling.Modules.Payments.Features.RegisterManual;
using EcoBilling.Modules.Reports.Features.GetFinancialSummary;
using EcoBilling.Modules.Reports.Features.GetOperationalSummary;
using EcoBilling.Modules.Accounts.Features.GetAddressById;
using EcoBilling.Modules.Accounts.Features.GetByNumber;
using EcoBilling.Modules.Residents.Features.CreateResident;
using EcoBilling.Modules.Residents.Features.ResetPassword;
using EcoBilling.Modules.Residents.Features.GetProfile;
using EcoBilling.Modules.Residents.Features.SelfService;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var useLocalConfiguration =
    builder.Configuration.GetValue<bool>("LocalConfiguration:Enabled") ||
    Debugger.IsAttached;
if (useLocalConfiguration)
{
    builder.Configuration.AddJsonFile(
        "appsettings.Local.json",
        optional: true,
        reloadOnChange: true);
}

builder.Logging.AddEcoBillingStructuredLogging(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("EcoBilling");
if (string.IsNullOrWhiteSpace(connectionString) &&
    useLocalConfiguration)
{
    builder.Configuration.AddUserSecrets<Program>(
        optional: true,
        reloadOnChange: true);
    connectionString = builder.Configuration.GetConnectionString("EcoBilling");
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'EcoBilling' is required. Configure it without committing secrets, for example through ConnectionStrings__EcoBilling.");
}

var requestFingerprintKey = builder.Configuration[
    "DirectorProvisioning:RequestFingerprintKey"];
if (string.IsNullOrWhiteSpace(requestFingerprintKey))
{
    throw new InvalidOperationException(
        "Director provisioning request fingerprint key is required. Configure DirectorProvisioning__RequestFingerprintKey with at least 32 random bytes encoded as Base64.");
}

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "EcoBilling API",
            Description = "Backend API for one EcoBilling district.",
            Version = "v1"
        });
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter the access token returned by POST /api/v1/auth/login."
        });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions.TryAdd(
            "code",
            context.ProblemDetails.Status switch
            {
                StatusCodes.Status400BadRequest => "validation.failed",
                StatusCodes.Status401Unauthorized => "service.unauthorized",
                StatusCodes.Status403Forbidden => "service.forbidden",
                StatusCodes.Status404NotFound => "request.not_found",
                _ => "server.error"
            });

        if (context.ProblemDetails.Status is StatusCodes.Status400BadRequest)
        {
            context.ProblemDetails.Extensions.TryAdd(
                "validationErrors",
                new Dictionary<string, string[]>
                {
                    ["request"] = ["The request body is invalid."]
                });
        }
    };
});
builder.Services.AddEcoBillingInfrastructure(connectionString);
builder.Services.AddEcoBillingObservability(
    builder.Configuration,
    EcoBillingTelemetry.ApiServiceName,
    instrumentAspNetCore: true);
builder.Services.AddDirectorProvisioningSecurity(requestFingerprintKey);
builder.Services.AddInternalServiceAuthentication(builder.Configuration);
builder.Services.AddUserAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ProvisionDirectorHandler>();
builder.Services.AddScoped<CreateControllerHandler>();
builder.Services.AddScoped<AssignAddressHandler>();
builder.Services.AddScoped<RemoveAssignmentHandler>();
builder.Services.AddScoped<GetMyAssignmentsHandler>();
builder.Services.AddScoped<GetControllerProfileHandler>();
builder.Services.AddScoped<GetControllerWorklistHandler>();
builder.Services.AddScoped<CreateResidentHandler>();
builder.Services.AddScoped<ResetResidentPasswordHandler>();
builder.Services.AddScoped<GetResidentProfileHandler>();
builder.Services.AddScoped<GetResidentAccountHandler>();
builder.Services.AddScoped<ListResidentMetersHandler>();
builder.Services.AddScoped<ListResidentReadingsHandler>();
builder.Services.AddScoped<ListResidentChargesHandler>();
builder.Services.AddScoped<ListResidentPaymentsHandler>();
builder.Services.AddScoped<GetResidentFinancialHandler>();
builder.Services.AddScoped<CreateMeterHandler>();
builder.Services.AddScoped<ReplaceMeterHandler>();
builder.Services.AddScoped<ListMetersByAccountHandler>();
builder.Services.AddScoped<AddMeterReadingHandler>();
builder.Services.AddScoped<ListReadingsByMeterHandler>();
builder.Services.AddScoped<CreateTariffHandler>();
builder.Services.AddScoped<CreateTariffVersionHandler>();
builder.Services.AddScoped<AssignTariffToAccountHandler>();
builder.Services.AddScoped<ListTariffsHandler>();
builder.Services.AddScoped<ListTariffVersionsHandler>();
builder.Services.AddScoped<CalculateMonthlyChargeHandler>();
builder.Services.AddScoped<ListChargesByAccountHandler>();
builder.Services.AddScoped<RegisterManualPaymentHandler>();
builder.Services.AddScoped<ListPaymentsByAccountHandler>();
builder.Services.AddScoped<GetAccountFinancialSummaryHandler>();
builder.Services.AddScoped<GetDistrictOperationalSummaryHandler>();
builder.Services.AddScoped<GetDistrictFinancialSummaryHandler>();
builder.Services.AddScoped<GetAccountByNumberHandler>();
builder.Services.AddScoped<GetAddressByIdHandler>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestObservabilityMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "EcoBilling API";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EcoBilling API v1");
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapEcoBillingHealthEndpoints();

app.MapDirectorProvisioningEndpoints();
app.MapUserAuthenticationEndpoints();
app.MapControllerManagementEndpoints();
app.MapAccountManagementEndpoints();
app.MapResidentManagementEndpoints();
app.MapResidentSelfServiceEndpoints();
app.MapMeterManagementEndpoints();
app.MapReadingEndpoints();
app.MapTariffManagementEndpoints();
app.MapBillingEndpoints();
app.MapPaymentEndpoints();
app.MapReportEndpoints();

app.Run();

public partial class Program;
