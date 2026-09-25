using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Residents.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace EcoBilling.IntegrationTests.Meters;

public sealed class MetersPersistenceTests
{
    private const string AddressesMigration = "20260925065110_AddAddresses";

    [PostgreSqlFact]
    public async Task Migration_CreatesMetersSchemaTableAndConstraints()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                to_regnamespace('meters')::text,
                to_regclass('meters.meters')::text,
                (SELECT count(*) FROM pg_constraint
                    WHERE conrelid = 'meters.meters'::regclass),
                (SELECT count(*) FROM pg_indexes
                    WHERE schemaname = 'meters' AND tablename = 'meters'),
                (SELECT count(*) FROM information_schema.columns
                    WHERE table_schema = 'meters'
                      AND table_name = 'meters'
                      AND is_nullable = 'NO')
            """;
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("meters", reader.GetString(0));
        Assert.Equal("meters.meters", reader.GetString(1));
        Assert.Equal(2, reader.GetInt64(2));
        Assert.Equal(2, reader.GetInt64(3));
        Assert.Equal(5, reader.GetInt64(4));
    }

    [PostgreSqlFact]
    public async Task Repository_LoadsMeterById()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var graph = CreateAccountGraph("AB-123");
        var installedAt = new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(6));
        var createdAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(6));
        var meter = Meter.Create(
            new MeterId(Guid.NewGuid()),
            graph.Account.Id,
            "  sn-ab-123  ",
            installedAt,
            createdAt).Value;
        await SaveAsync(database, graph, meter);
        await using var context = database.CreateContext();
        var repository = new MeterRepository(context);

        var loaded = await repository.GetByIdAsync(meter.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(graph.Account.Id, loaded.AccountId);
        Assert.Equal("SN-AB-123", loaded.SerialNumber.Value);
        Assert.Equal(installedAt.ToUniversalTime(), loaded.InstalledAt);
        Assert.Equal(createdAt.ToUniversalTime(), loaded.CreatedAt);
    }

    [PostgreSqlFact]
    public async Task Repository_UnknownId_ReturnsNull()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new MeterRepository(context);

        var loaded = await repository.GetByIdAsync(
            new MeterId(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(loaded);
    }

    [PostgreSqlFact]
    public async Task MissingAccount_IsRejectedByForeignKey()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var meter = Meter.Create(
            new MeterId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            "SN-AB-123",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value;
        await using var context = database.CreateContext();
        context.Meters.Add(meter);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task MultipleMetersWithEquivalentSerialNumbers_AreAllowedForOneAccount()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var graph = CreateAccountGraph("AB-123");
        await SaveAccountGraphAsync(database, graph);
        await using var context = database.CreateContext();
        context.Meters.Add(
            Meter.Create(
                new MeterId(Guid.NewGuid()),
                graph.Account.Id,
                "sn-ab-123",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow).Value);
        context.Meters.Add(
            Meter.Create(
                new MeterId(Guid.NewGuid()),
                graph.Account.Id,
                " SN-AB-123 ",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow).Value);

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Meters.CountAsync());
    }

    [PostgreSqlFact]
    public async Task ReferencedAccount_CannotBeDeleted()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var graph = CreateAccountGraph("AB-123");
        var meter = CreateMeter(graph.Account);
        await SaveAsync(database, graph, meter);
        await using var context = database.CreateContext();
        context.Accounts.Remove(graph.Account);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task Repository_ForwardsCanceledToken()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new MeterRepository(context);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByIdAsync(
                new MeterId(Guid.NewGuid()),
                cancellationTokenSource.Token));
    }

    [PostgreSqlFact]
    public async Task Migration_CanRollbackMetersAndReapply()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(AddressesMigration);
        Assert.Null(await FindSchemaAsync(database.ConnectionString, "meters"));
        Assert.Equal("accounts", await FindSchemaAsync(database.ConnectionString, "accounts"));

        await migrator.MigrateAsync();
        Assert.Equal("meters", await FindSchemaAsync(database.ConnectionString, "meters"));
    }

    private static async Task<PostgreSqlTestDatabase> CreateMigratedDatabaseAsync()
    {
        var database = await PostgreSqlTestDatabase.CreateAsync();

        try
        {
            await using var context = database.CreateContext();
            await context.Database.MigrateAsync();
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    private static AccountGraph CreateAccountGraph(string accountNumber)
    {
        var userAccount = CreateResidentIdentity(accountNumber);
        var resident = Resident.Create(
            new ResidentId(Guid.NewGuid()),
            userAccount,
            "Ada Lovelace",
            DateTimeOffset.UtcNow).Value;
        var address = Address.Create(
            new AddressId(Guid.NewGuid()),
            "Бишкек",
            "Исанова",
            "10",
            null,
            "42").Value;
        var account = Account.Create(
            new AccountId(Guid.NewGuid()),
            resident.Id,
            address.Id,
            accountNumber,
            DateTimeOffset.UtcNow).Value;

        return new AccountGraph(userAccount, resident, address, account);
    }

    private static UserAccount CreateResidentIdentity(string accountNumber)
    {
        var loginIdentity = LoginIdentity.Create(LoginType.AccountNumber, accountNumber).Value;
        return UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity,
            "stored-password-hash",
            UserRole.Resident,
            DateTimeOffset.UtcNow).Value;
    }

    private static Meter CreateMeter(Account account) =>
        Meter.Create(
            new MeterId(Guid.NewGuid()),
            account.Id,
            "SN-AB-123",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow).Value;

    private static async Task SaveAsync(
        PostgreSqlTestDatabase database,
        AccountGraph graph,
        Meter meter)
    {
        await SaveAccountGraphAsync(database, graph);
        await using var context = database.CreateContext();
        context.Meters.Add(meter);
        await context.SaveChangesAsync();
    }

    private static async Task SaveAccountGraphAsync(
        PostgreSqlTestDatabase database,
        AccountGraph graph)
    {
        await using var context = database.CreateContext();
        context.UserAccounts.Add(graph.UserAccount);
        await context.SaveChangesAsync();
        context.Residents.Add(graph.Resident);
        context.Addresses.Add(graph.Address);
        await context.SaveChangesAsync();
        context.Accounts.Add(graph.Account);
        await context.SaveChangesAsync();
    }

    private static async Task<string?> FindSchemaAsync(
        string connectionString,
        string schema)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regnamespace(@schema)::text";
        command.Parameters.AddWithValue("schema", schema);
        return await command.ExecuteScalarAsync() as string;
    }

    private sealed record AccountGraph(
        UserAccount UserAccount,
        Resident Resident,
        Address Address,
        Account Account);
}
