using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EcoBilling.Infrastructure.Persistence;

public sealed class EcoBillingDbContextFactory : IDesignTimeDbContextFactory<EcoBillingDbContext>
{
    public const string ConnectionStringEnvironmentVariable =
        "ECOBILLING_DESIGN_TIME_CONNECTION";

    public EcoBillingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Environment variable '{ConnectionStringEnvironmentVariable}' is required for design-time operations.");
        }

        var options = new DbContextOptionsBuilder<EcoBillingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new EcoBillingDbContext(options);
    }
}
