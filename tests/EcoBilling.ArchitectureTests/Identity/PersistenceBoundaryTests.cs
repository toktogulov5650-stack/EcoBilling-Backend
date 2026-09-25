using EcoBilling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Reflection;

namespace EcoBilling.ArchitectureTests.Identity;

public sealed class PersistenceBoundaryTests
{
    [Fact]
    public void DbContextTypes_LiveOnlyInInfrastructure()
    {
        var dbContextTypes = LoadProductionAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(DbContext).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(dbContextTypes);
        Assert.All(
            dbContextTypes,
            type => Assert.Equal(typeof(EcoBillingDbContext).Assembly, type.Assembly));
    }

    [Fact]
    public void MigrationTypes_LiveOnlyInInfrastructure()
    {
        var migrationTypes = LoadProductionAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(Migration).IsAssignableFrom(type) && !type.IsAbstract)
            .ToArray();

        Assert.NotEmpty(migrationTypes);
        Assert.All(
            migrationTypes,
            type =>
            {
                Assert.Equal(typeof(EcoBillingDbContext).Assembly, type.Assembly);
                Assert.StartsWith(
                    "EcoBilling.Infrastructure.Migrations",
                    type.Namespace,
                    StringComparison.Ordinal);
            });
    }

    private static Assembly[] LoadProductionAssemblies() =>
        [
            Assembly.Load("EcoBilling.Api"),
            Assembly.Load("EcoBilling.Infrastructure"),
            Assembly.Load("EcoBilling.Modules"),
            Assembly.Load("EcoBilling.SharedKernel"),
            Assembly.Load("EcoBilling.Worker")
        ];
}
