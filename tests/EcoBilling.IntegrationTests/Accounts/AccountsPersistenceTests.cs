using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace EcoBilling.IntegrationTests.Accounts;

public sealed class AccountsPersistenceTests
{
    private const string ControllersMigration =
        "20260925055752_AddControllersProfile";

    [PostgreSqlFact]
    public async Task Migration_CreatesAccountsSchemaTableAndConstraints()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                to_regnamespace('accounts')::text,
                to_regclass('accounts.accounts')::text,
                (SELECT count(*) FROM pg_constraint
                    WHERE conrelid = 'accounts.accounts'::regclass),
                (SELECT count(*) FROM pg_indexes
                    WHERE schemaname = 'accounts' AND tablename = 'accounts')
            """;
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("accounts", reader.GetString(0));
        Assert.Equal("accounts.accounts", reader.GetString(1));
        Assert.Equal(3, reader.GetInt64(2));
        Assert.Equal(4, reader.GetInt64(3));
    }

    [PostgreSqlFact]
    public async Task Repository_LoadsAccountByNormalizedNumber()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var userAccount = CreateResidentIdentity("ab-123");
        var resident = CreateResident(userAccount);
        var address = CreateAddress();
        var account = CreateAccount(resident, address, "  ab-123  ");
        await SaveAsync(database, userAccount, resident, address, account);
        await using var context = database.CreateContext();
        var repository = new AccountRepository(context);

        var loaded = await repository.GetByNumberAsync(
            AccountNumber.Create(" AB-123 ").Value,
            CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(account.Id, loaded.Id);
        Assert.Equal(resident.Id, loaded.ResidentId);
        Assert.Equal(address.Id, loaded.AddressId);
        Assert.Equal("AB-123", loaded.Number.Value);
    }

    [PostgreSqlFact]
    public async Task Repository_UnknownNumber_ReturnsNull()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new AccountRepository(context);

        var loaded = await repository.GetByNumberAsync(
            AccountNumber.Create("AB-404").Value,
            CancellationToken.None);

        Assert.Null(loaded);
    }

    [PostgreSqlFact]
    public async Task DuplicateNormalizedNumber_IsRejectedByDatabase()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var firstIdentity = CreateResidentIdentity("duplicate-1");
        var secondIdentity = CreateResidentIdentity("duplicate-2");
        var firstResident = CreateResident(firstIdentity);
        var secondResident = CreateResident(secondIdentity);
        var firstAddress = CreateAddress();
        var secondAddress = CreateAddress();
        await using var context = database.CreateContext();
        context.UserAccounts.AddRange(firstIdentity, secondIdentity);
        await context.SaveChangesAsync();
        context.Residents.AddRange(firstResident, secondResident);
        await context.SaveChangesAsync();
        context.Addresses.AddRange(firstAddress, secondAddress);
        await context.SaveChangesAsync();
        context.Accounts.Add(CreateAccount(firstResident, firstAddress, "ab-123"));
        context.Accounts.Add(CreateAccount(secondResident, secondAddress, " AB-123 "));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task MissingResident_IsRejectedByForeignKey()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var address = CreateAddress();
        var account = Account.Create(
            new AccountId(Guid.NewGuid()),
            new ResidentId(Guid.NewGuid()),
            address.Id,
            "AB-123",
            DateTimeOffset.UtcNow).Value;
        await using var context = database.CreateContext();
        context.Addresses.Add(address);
        await context.SaveChangesAsync();
        context.Accounts.Add(account);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task Repository_ForwardsCanceledToken()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new AccountRepository(context);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByNumberAsync(
                AccountNumber.Create("AB-123").Value,
                cancellationTokenSource.Token));
    }

    [PostgreSqlFact]
    public async Task Migration_CanRollbackAccountsAndReapply()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(ControllersMigration);
        Assert.Null(await FindSchemaAsync(database.ConnectionString, "accounts"));
        Assert.Equal("controllers", await FindSchemaAsync(database.ConnectionString, "controllers"));

        await migrator.MigrateAsync();
        Assert.Equal("accounts", await FindSchemaAsync(database.ConnectionString, "accounts"));
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

    private static async Task SaveAsync(
        PostgreSqlTestDatabase database,
        UserAccount userAccount,
        Resident resident,
        Address address,
        Account account)
    {
        await using var context = database.CreateContext();
        context.UserAccounts.Add(userAccount);
        await context.SaveChangesAsync();
        context.Residents.Add(resident);
        await context.SaveChangesAsync();
        context.Addresses.Add(address);
        await context.SaveChangesAsync();
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
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

    private static Resident CreateResident(UserAccount userAccount) =>
        Resident.Create(
            new ResidentId(Guid.NewGuid()),
            userAccount,
            "Ada Lovelace",
            DateTimeOffset.UtcNow).Value;

    private static Address CreateAddress() =>
        Address.Create(
            new AddressId(Guid.NewGuid()),
            "Бишкек",
            "Исанова",
            "10",
            null,
            "42").Value;

    private static Account CreateAccount(
        Resident resident,
        Address address,
        string accountNumber) =>
        Account.Create(
            new AccountId(Guid.NewGuid()),
            resident.Id,
            address.Id,
            accountNumber,
            DateTimeOffset.UtcNow).Value;

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
}
