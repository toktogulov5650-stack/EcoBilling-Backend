using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Tariffs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace EcoBilling.IntegrationTests.Billing;

public sealed class BillingPersistenceTests
{
    private const string TariffsMigration = "20260925115307_AddTariffs";

    [PostgreSqlFact]
    public async Task Migration_CreatesBillingSchemaTableConstraintsAndIndexes()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                to_regnamespace('billing')::text,
                to_regclass('billing.charges')::text,
                (SELECT count(*) FROM pg_constraint
                    WHERE conrelid = 'billing.charges'::regclass),
                (SELECT count(*) FROM pg_indexes
                    WHERE schemaname = 'billing' AND tablename = 'charges'),
                (SELECT count(*) FROM information_schema.columns
                    WHERE table_schema = 'billing'
                      AND table_name = 'charges'
                      AND is_nullable = 'NO'),
                (SELECT data_type FROM information_schema.columns
                    WHERE table_schema = 'billing'
                      AND table_name = 'charges'
                      AND column_name = 'amount')
            """;
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("billing", reader.GetString(0));
        Assert.Equal("billing.charges", reader.GetString(1));
        Assert.Equal(4, reader.GetInt64(2));
        Assert.Equal(3, reader.GetInt64(3));
        Assert.Equal(7, reader.GetInt64(4));
        Assert.Equal("numeric", reader.GetString(5));
    }

    [PostgreSqlFact]
    public async Task Repository_LoadsChargeByIdWithExactSignedAmount()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var accountGraph = CreateAccountGraph("AB-123");
        var tariff = CreateTariff("Население");
        var version = CreateTariffVersion(tariff);
        var createdAt = new DateTimeOffset(2026, 2, 2, 10, 0, 0, TimeSpan.FromHours(6));
        var charge = Charge.Create(
            new ChargeId(Guid.NewGuid()),
            accountGraph.Account.Id,
            version.Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1),
            -123456789.123456789m,
            createdAt).Value;
        await SaveAsync(database, accountGraph, tariff, version, charge);
        await using var context = database.CreateContext();
        var repository = new ChargeRepository(context);

        var loaded = await repository.GetByIdAsync(charge.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(accountGraph.Account.Id, loaded.AccountId);
        Assert.Equal(version.Id, loaded.TariffVersionId);
        Assert.Equal(new DateOnly(2026, 1, 1), loaded.PeriodStart);
        Assert.Equal(new DateOnly(2026, 2, 1), loaded.PeriodEnd);
        Assert.Equal(-123456789.123456789m, loaded.Amount);
        Assert.Equal(createdAt.ToUniversalTime(), loaded.CreatedAt);
    }

    [PostgreSqlFact]
    public async Task Repository_UnknownId_ReturnsNull()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new ChargeRepository(context);

        var loaded = await repository.GetByIdAsync(
            new ChargeId(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(loaded);
    }

    [PostgreSqlFact]
    public async Task MissingAccount_IsRejectedByForeignKey()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var tariff = CreateTariff("Население");
        var version = CreateTariffVersion(tariff);
        await SaveTariffAsync(database, tariff, version);
        var charge = CreateCharge(new AccountId(Guid.NewGuid()), version.Id);
        await using var context = database.CreateContext();
        context.Charges.Add(charge);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task MissingTariffVersion_IsRejectedByForeignKey()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var accountGraph = CreateAccountGraph("AB-123");
        await SaveAccountGraphAsync(database, accountGraph);
        var charge = CreateCharge(
            accountGraph.Account.Id,
            new TariffVersionId(Guid.NewGuid()));
        await using var context = database.CreateContext();
        context.Charges.Add(charge);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task InvalidPeriod_IsRejectedByDatabaseCheckConstraint()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var accountGraph = CreateAccountGraph("AB-123");
        var tariff = CreateTariff("Население");
        var version = CreateTariffVersion(tariff);
        await SavePrerequisitesAsync(database, accountGraph, tariff, version);
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO billing.charges
                (id, account_id, tariff_version_id, period_start, period_end, amount, created_at)
            VALUES
                (@id, @account_id, @tariff_version_id, @period_start, @period_end, @amount, @created_at)
            """;
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("account_id", accountGraph.Account.Id.Value);
        command.Parameters.AddWithValue("tariff_version_id", version.Id.Value);
        command.Parameters.AddWithValue("period_start", new DateOnly(2026, 2, 1));
        command.Parameters.AddWithValue("period_end", new DateOnly(2026, 1, 1));
        command.Parameters.AddWithValue("amount", 100m);
        command.Parameters.AddWithValue("created_at", DateTimeOffset.UtcNow);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("ck_charges_billing_period", exception.ConstraintName);
    }

    [PostgreSqlFact]
    public async Task DuplicatePeriod_ForSameAccount_IsRejected()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var accountGraph = CreateAccountGraph("AB-123");
        var tariff = CreateTariff("Население");
        var version = CreateTariffVersion(tariff);
        await SavePrerequisitesAsync(database, accountGraph, tariff, version);
        await using var context = database.CreateContext();
        context.Charges.Add(CreateCharge(accountGraph.Account.Id, version.Id));
        context.Charges.Add(CreateCharge(accountGraph.Account.Id, version.Id));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
        Assert.Equal(
            "ux_charges_account_id_period_start_period_end",
            postgresException.ConstraintName);
    }

    [PostgreSqlFact]
    public async Task SamePeriod_ForDifferentAccounts_IsAllowed()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var firstAccount = CreateAccountGraph("AB-123");
        var secondAccount = CreateAccountGraph("CD-456");
        var tariff = CreateTariff("Население");
        var version = CreateTariffVersion(tariff);
        await SaveAccountGraphAsync(database, firstAccount);
        await SaveAccountGraphAsync(database, secondAccount);
        await SaveTariffAsync(database, tariff, version);
        await using var context = database.CreateContext();
        context.Charges.Add(CreateCharge(firstAccount.Account.Id, version.Id));
        context.Charges.Add(CreateCharge(secondAccount.Account.Id, version.Id));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Charges.CountAsync());
    }

    [PostgreSqlFact]
    public async Task AdjacentPeriods_ForSameAccount_AreAllowed()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var accountGraph = CreateAccountGraph("AB-123");
        var tariff = CreateTariff("Население");
        var version = CreateTariffVersion(tariff);
        await SavePrerequisitesAsync(database, accountGraph, tariff, version);
        await using var context = database.CreateContext();
        context.Charges.Add(
            CreateCharge(
                accountGraph.Account.Id,
                version.Id,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 2, 1)));
        context.Charges.Add(
            CreateCharge(
                accountGraph.Account.Id,
                version.Id,
                new DateOnly(2026, 2, 1),
                new DateOnly(2026, 3, 1)));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Charges.CountAsync());
    }

    [PostgreSqlFact]
    public async Task ReferencedAccount_CannotBeDeleted()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var accountGraph = CreateAccountGraph("AB-123");
        var tariff = CreateTariff("Население");
        var version = CreateTariffVersion(tariff);
        var charge = CreateCharge(accountGraph.Account.Id, version.Id);
        await SaveAsync(database, accountGraph, tariff, version, charge);
        await using var context = database.CreateContext();
        context.Accounts.Remove(accountGraph.Account);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task ReferencedTariffVersion_CannotBeDeleted()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var accountGraph = CreateAccountGraph("AB-123");
        var tariff = CreateTariff("Население");
        var version = CreateTariffVersion(tariff);
        var charge = CreateCharge(accountGraph.Account.Id, version.Id);
        await SaveAsync(database, accountGraph, tariff, version, charge);
        await using var context = database.CreateContext();
        context.TariffVersions.Remove(version);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task Repository_ForwardsCanceledToken()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new ChargeRepository(context);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByIdAsync(
                new ChargeId(Guid.NewGuid()),
                cancellationTokenSource.Token));
    }

    [PostgreSqlFact]
    public async Task Migration_CanRollbackBillingAndReapply()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(TariffsMigration);
        Assert.Null(await FindSchemaAsync(database.ConnectionString, "billing"));
        Assert.Equal("tariffs", await FindSchemaAsync(database.ConnectionString, "tariffs"));

        await migrator.MigrateAsync();
        Assert.Equal("billing", await FindSchemaAsync(database.ConnectionString, "billing"));
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
        var loginIdentity = LoginIdentity.Create(LoginType.AccountNumber, accountNumber).Value;
        var userAccount = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity,
            "stored-password-hash",
            UserRole.Resident,
            DateTimeOffset.UtcNow).Value;
        var resident = Resident.Create(
            new ResidentId(Guid.NewGuid()),
            userAccount,
            $"Resident {accountNumber}",
            DateTimeOffset.UtcNow).Value;
        var address = Address.Create(
            new AddressId(Guid.NewGuid()),
            "Бишкек",
            "Исанова",
            accountNumber,
            null,
            null).Value;
        var account = Account.Create(
            new AccountId(Guid.NewGuid()),
            resident.Id,
            address.Id,
            accountNumber,
            DateTimeOffset.UtcNow).Value;

        return new AccountGraph(userAccount, resident, address, account);
    }

    private static Tariff CreateTariff(string name) =>
        Tariff.Create(
            new TariffId(Guid.NewGuid()),
            name,
            DateTimeOffset.UtcNow).Value;

    private static TariffVersion CreateTariffVersion(Tariff tariff) =>
        TariffVersion.Create(
            new TariffVersionId(Guid.NewGuid()),
            tariff.Id,
            12.345m,
            new DateOnly(2026, 1, 1),
            null,
            DateTimeOffset.UtcNow).Value;

    private static Charge CreateCharge(
        AccountId accountId,
        TariffVersionId tariffVersionId,
        DateOnly? periodStart = null,
        DateOnly? periodEnd = null) =>
        Charge.Create(
            new ChargeId(Guid.NewGuid()),
            accountId,
            tariffVersionId,
            periodStart ?? new DateOnly(2026, 1, 1),
            periodEnd ?? new DateOnly(2026, 2, 1),
            100m,
            DateTimeOffset.UtcNow).Value;

    private static async Task SaveAsync(
        PostgreSqlTestDatabase database,
        AccountGraph accountGraph,
        Tariff tariff,
        TariffVersion version,
        Charge charge)
    {
        await SavePrerequisitesAsync(database, accountGraph, tariff, version);
        await using var context = database.CreateContext();
        context.Charges.Add(charge);
        await context.SaveChangesAsync();
    }

    private static async Task SavePrerequisitesAsync(
        PostgreSqlTestDatabase database,
        AccountGraph accountGraph,
        Tariff tariff,
        TariffVersion version)
    {
        await SaveAccountGraphAsync(database, accountGraph);
        await SaveTariffAsync(database, tariff, version);
    }

    private static async Task SaveTariffAsync(
        PostgreSqlTestDatabase database,
        Tariff tariff,
        TariffVersion version)
    {
        await using var context = database.CreateContext();
        context.Tariffs.Add(tariff);
        await context.SaveChangesAsync();
        context.TariffVersions.Add(version);
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
