using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Infrastructure.Concurrency;
using EcoBilling.Infrastructure.Outbox;
using EcoBilling.Infrastructure.Observability;
using EcoBilling.Infrastructure.Persistence;
using EcoBilling.Infrastructure.Persistence.Reports;
using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.Modules.Accounts.Features.Abstractions;
using EcoBilling.Modules.Billing.Features.Abstractions;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Meters.Features.Abstractions;
using EcoBilling.Modules.Payments.Features.Abstractions;
using EcoBilling.Modules.Readings.Features.Abstractions;
using EcoBilling.Modules.Reports.Features.Abstractions;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EcoBilling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEcoBillingInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddSingleton(serviceProvider =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString)
            {
                Name = "EcoBilling"
            };
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            if (loggerFactory is not null)
            {
                dataSourceBuilder.UseLoggerFactory(loggerFactory);
            }

            return dataSourceBuilder.Build();
        });
        services.AddDbContext<EcoBillingDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>())
                .EnableSensitiveDataLogging(false));
        services
            .AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                tags: ["live"])
            .AddCheck<PostgreSqlReadinessHealthCheck>(
                "postgresql",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"],
                timeout: TimeSpan.FromSeconds(5));
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IDirectorProvisioningRepository, DirectorProvisioningRepository>();
        services.AddScoped<IRefreshSessionRepository, RefreshSessionRepository>();
        services.AddScoped<IInternalServiceTokenReplayStore, InternalServiceTokenReplayStore>();
        services.AddScoped<IResidentRepository, ResidentRepository>();
        services.AddScoped<IResidentCreationRepository, ResidentCreationRepository>();
        services.AddScoped<IResidentPasswordResetRepository, ResidentPasswordResetRepository>();
        services.AddScoped<IControllerRepository, ControllerRepository>();
        services.AddScoped<IControllerCreationRepository, ControllerCreationRepository>();
        services.AddScoped<IControllerAssignmentRepository, ControllerAssignmentRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IMeterRepository, MeterRepository>();
        services.AddScoped<IMeterReadingRepository, MeterReadingRepository>();
        services.AddScoped<ITariffRepository, TariffRepository>();
        services.AddScoped<ITariffVersionRepository, TariffVersionRepository>();
        services.AddScoped<IAccountTariffAssignmentRepository, AccountTariffAssignmentRepository>();
        services.AddScoped<IChargeRepository, ChargeRepository>();
        services.AddScoped<IBillingCalculationRepository, BillingCalculationRepository>();
        services.AddScoped<IMonthlyBillingBatchProcessor, MonthlyBillingBatchProcessor>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IDistrictOperationalSummaryReader, DistrictOperationalSummaryReader>();
        services.AddScoped<IDistrictFinancialSummaryReader, DistrictFinancialSummaryReader>();
        services.AddScoped<IResidentSelfServiceReader, ResidentSelfServiceReader>();
        services.AddScoped<IControllerWorklistReader, ControllerWorklistReader>();
        services.AddScoped<IControllerDirectoryReader, ControllerDirectoryReader>();
        services.AddScoped<IResidentDirectoryReader, ResidentDirectoryReader>();
        services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();
        services.AddSingleton<IWorkerExecutionLock, PostgreSqlWorkerExecutionLock>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();

        return services;
    }

    public static IServiceCollection AddDirectorProvisioningSecurity(
        this IServiceCollection services,
        string requestFingerprintKey)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestFingerprintKey);

        services.AddSingleton<IDirectorProvisioningRequestFingerprinter>(
            new DirectorProvisioningRequestFingerprinter(requestFingerprintKey));
        services.AddSingleton<IControllerCreationRequestFingerprinter>(
            new ControllerCreationRequestFingerprinter(requestFingerprintKey));
        services.AddSingleton<IResidentCreationRequestFingerprinter>(
            new ResidentCreationRequestFingerprinter(requestFingerprintKey));
        services.AddSingleton<IResidentPasswordResetRequestFingerprinter>(
            new ResidentPasswordResetRequestFingerprinter(requestFingerprintKey));

        return services;
    }
}
