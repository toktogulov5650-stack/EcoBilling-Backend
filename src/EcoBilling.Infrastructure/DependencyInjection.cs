using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Infrastructure.Persistence;
using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.Modules.Accounts.Features.Abstractions;
using EcoBilling.Modules.Billing.Features.Abstractions;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Meters.Features.Abstractions;
using EcoBilling.Modules.Payments.Features.Abstractions;
using EcoBilling.Modules.Readings.Features.Abstractions;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcoBilling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEcoBillingInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<EcoBillingDbContext>(options =>
            options
                .UseNpgsql(connectionString)
                .EnableSensitiveDataLogging(false));
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IResidentRepository, ResidentRepository>();
        services.AddScoped<IControllerRepository, ControllerRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IMeterRepository, MeterRepository>();
        services.AddScoped<IMeterReadingRepository, MeterReadingRepository>();
        services.AddScoped<ITariffRepository, TariffRepository>();
        services.AddScoped<ITariffVersionRepository, TariffVersionRepository>();
        services.AddScoped<IChargeRepository, ChargeRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();

        return services;
    }
}
