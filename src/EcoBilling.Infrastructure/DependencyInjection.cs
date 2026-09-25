using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Infrastructure.Persistence;
using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Residents.Features.Abstractions;
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
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();

        return services;
    }
}
