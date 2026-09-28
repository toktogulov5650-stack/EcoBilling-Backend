using EcoBilling.Api.Configuration;
using EcoBilling.Api.Endpoints;
using EcoBilling.Api.InternalEndpoints;
using EcoBilling.Api.Middleware;
using EcoBilling.Infrastructure;
using EcoBilling.Infrastructure.Observability;
using EcoBilling.Modules.Identity.Application.ProvisionDirector;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddEcoBillingStructuredLogging(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("EcoBilling");
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

app.Run();

public partial class Program;
