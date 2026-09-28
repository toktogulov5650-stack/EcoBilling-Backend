using EcoBilling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EcoBilling.EndToEndTests.Infrastructure;

internal sealed class PostgreSqlEndToEndDatabase : IAsyncDisposable
{
    private readonly string adminConnectionString;
    private readonly string databaseName;

    private PostgreSqlEndToEndDatabase(
        string adminConnectionString,
        string databaseName,
        string connectionString)
    {
        this.adminConnectionString = adminConnectionString;
        this.databaseName = databaseName;
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public static async Task<PostgreSqlEndToEndDatabase> CreateAsync()
    {
        var sourceBuilder = new NpgsqlConnectionStringBuilder(
            PostgreSqlEndToEndFactAttribute.GetConnectionString());
        var databaseName = $"ecobilling_e2e_{Guid.NewGuid():N}";
        var adminBuilder = new NpgsqlConnectionStringBuilder(
            sourceBuilder.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };
        var testBuilder = new NpgsqlConnectionStringBuilder(
            sourceBuilder.ConnectionString)
        {
            Database = databaseName
        };

        await using var connection = new NpgsqlConnection(
            adminBuilder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)}";
        await command.ExecuteNonQueryAsync();

        return new PostgreSqlEndToEndDatabase(
            adminBuilder.ConnectionString,
            databaseName,
            testBuilder.ConnectionString);
    }

    public EcoBillingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EcoBillingDbContext>()
            .UseNpgsql(ConnectionString)
            .EnableSensitiveDataLogging(false)
            .Options;

        return new EcoBillingDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"DROP DATABASE IF EXISTS {QuoteIdentifier(databaseName)} WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }

    private static string QuoteIdentifier(string identifier)
    {
        using var commandBuilder = new NpgsqlCommandBuilder();
        return commandBuilder.QuoteIdentifier(identifier);
    }
}
