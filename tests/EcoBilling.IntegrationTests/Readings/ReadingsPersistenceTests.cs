using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Readings.Domain;
using EcoBilling.Modules.Residents.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace EcoBilling.IntegrationTests.Readings;

public sealed class ReadingsPersistenceTests
{
    private const string MetersMigration = "20260925112021_AddMeters";

    [PostgreSqlFact]
    public async Task Migration_CreatesReadingsSchemaTableConstraintsAndIndex()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                to_regnamespace('readings')::text,
                to_regclass('readings.meter_readings')::text,
                (SELECT count(*) FROM pg_constraint
                    WHERE conrelid = 'readings.meter_readings'::regclass),
                (SELECT count(*) FROM pg_indexes
                    WHERE schemaname = 'readings' AND tablename = 'meter_readings'),
                (SELECT count(*) FROM information_schema.columns
                    WHERE table_schema = 'readings'
                      AND table_name = 'meter_readings'
                      AND is_nullable = 'NO')
            """;
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("readings", reader.GetString(0));
        Assert.Equal("readings.meter_readings", reader.GetString(1));
        Assert.Equal(3, reader.GetInt64(2));
        Assert.Equal(2, reader.GetInt64(3));
        Assert.Equal(5, reader.GetInt64(4));
    }

    [PostgreSqlFact]
    public async Task Repository_LoadsReadingByIdWithExactValueAndUtcTimestamps()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var graph = CreateAccountGraph("AB-123");
        var meter = CreateMeter(graph.Account);
        var measuredAt = new DateTimeOffset(2026, 9, 24, 18, 30, 0, TimeSpan.FromHours(6));
        var createdAt = new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.FromHours(6));
        var reading = MeterReading.Create(
            new MeterReadingId(Guid.NewGuid()),
            meter.Id,
            123456789.123456789m,
            measuredAt,
            createdAt).Value;
        await SaveAsync(database, graph, meter, reading);
        await using var context = database.CreateContext();
        var repository = new MeterReadingRepository(context);

        var loaded = await repository.GetByIdAsync(reading.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(meter.Id, loaded.MeterId);
        Assert.Equal(123456789.123456789m, loaded.Value.Value);
        Assert.Equal(measuredAt.ToUniversalTime(), loaded.MeasuredAt);
        Assert.Equal(createdAt.ToUniversalTime(), loaded.CreatedAt);
    }

    [PostgreSqlFact]
    public async Task Repository_UnknownId_ReturnsNull()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new MeterReadingRepository(context);

        var loaded = await repository.GetByIdAsync(
            new MeterReadingId(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(loaded);
    }

    [PostgreSqlFact]
    public async Task MissingMeter_IsRejectedByForeignKey()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var reading = MeterReading.Create(
            new MeterReadingId(Guid.NewGuid()),
            new MeterId(Guid.NewGuid()),
            10m,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow).Value;
        await using var context = database.CreateContext();
        context.MeterReadings.Add(reading);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task NegativeValue_IsRejectedByDatabaseCheckConstraint()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var graph = CreateAccountGraph("AB-123");
        var meter = CreateMeter(graph.Account);
        await SaveMeterAsync(database, graph, meter);
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO readings.meter_readings
                (id, meter_id, value, measured_at, created_at)
            VALUES
                (@id, @meter_id, @value, @measured_at, @created_at)
            """;
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("meter_id", meter.Id.Value);
        command.Parameters.AddWithValue("value", -1m);
        command.Parameters.AddWithValue("measured_at", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("created_at", DateTimeOffset.UtcNow);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("ck_meter_readings_value_non_negative", exception.ConstraintName);
    }

    [PostgreSqlFact]
    public async Task DecreasingValues_AreAllowedForOneMeter()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var graph = CreateAccountGraph("AB-123");
        var meter = CreateMeter(graph.Account);
        await SaveMeterAsync(database, graph, meter);
        await using var context = database.CreateContext();
        var firstMeasuredAt = DateTimeOffset.UtcNow.AddDays(-1);
        context.MeterReadings.Add(CreateReading(meter, 100m, firstMeasuredAt));
        context.MeterReadings.Add(CreateReading(meter, 90m, firstMeasuredAt.AddHours(1)));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.MeterReadings.CountAsync());
    }

    [PostgreSqlFact]
    public async Task DuplicateMeasurementTime_IsAllowedForOneMeter()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var graph = CreateAccountGraph("AB-123");
        var meter = CreateMeter(graph.Account);
        await SaveMeterAsync(database, graph, meter);
        await using var context = database.CreateContext();
        var measuredAt = DateTimeOffset.UtcNow.AddDays(-1);
        context.MeterReadings.Add(CreateReading(meter, 100m, measuredAt));
        context.MeterReadings.Add(CreateReading(meter, 101m, measuredAt));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.MeterReadings.CountAsync());
    }

    [PostgreSqlFact]
    public async Task ReferencedMeter_CannotBeDeleted()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var graph = CreateAccountGraph("AB-123");
        var meter = CreateMeter(graph.Account);
        var reading = CreateReading(meter, 100m, DateTimeOffset.UtcNow.AddDays(-1));
        await SaveAsync(database, graph, meter, reading);
        await using var context = database.CreateContext();
        context.Meters.Remove(meter);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task Repository_ForwardsCanceledToken()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new MeterReadingRepository(context);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByIdAsync(
                new MeterReadingId(Guid.NewGuid()),
                cancellationTokenSource.Token));
    }

    [PostgreSqlFact]
    public async Task Migration_CanRollbackReadingsAndReapply()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(MetersMigration);
        Assert.Null(await FindSchemaAsync(database.ConnectionString, "readings"));
        Assert.Equal("meters", await FindSchemaAsync(database.ConnectionString, "meters"));

        await migrator.MigrateAsync();
        Assert.Equal("readings", await FindSchemaAsync(database.ConnectionString, "readings"));
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
            DateTimeOffset.UtcNow.AddDays(-10),
            DateTimeOffset.UtcNow).Value;

    private static MeterReading CreateReading(
        Meter meter,
        decimal value,
        DateTimeOffset measuredAt) =>
        MeterReading.Create(
            new MeterReadingId(Guid.NewGuid()),
            meter.Id,
            value,
            measuredAt,
            DateTimeOffset.UtcNow).Value;

    private static async Task SaveAsync(
        PostgreSqlTestDatabase database,
        AccountGraph graph,
        Meter meter,
        MeterReading reading)
    {
        await SaveMeterAsync(database, graph, meter);
        await using var context = database.CreateContext();
        context.MeterReadings.Add(reading);
        await context.SaveChangesAsync();
    }

    private static async Task SaveMeterAsync(
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
