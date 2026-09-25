using EcoBilling.Infrastructure;
using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Infrastructure.Persistence;
using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Residents.Features.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EcoBilling.IntegrationTests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddEcoBillingInfrastructure_RegistersCurrentInfrastructureServices()
    {
        var services = new ServiceCollection();
        services.AddEcoBillingInfrastructure(
            "Host=127.0.0.1;Database=not-opened;Username=not-used;Password=not-used");
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        Assert.IsType<EcoBillingDbContext>(
            scope.ServiceProvider.GetRequiredService<EcoBillingDbContext>());
        Assert.IsType<UserAccountRepository>(
            scope.ServiceProvider.GetRequiredService<IUserAccountRepository>());
        Assert.IsType<ResidentRepository>(
            scope.ServiceProvider.GetRequiredService<IResidentRepository>());
        Assert.IsType<PasswordHasherAdapter>(
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
    }
}
