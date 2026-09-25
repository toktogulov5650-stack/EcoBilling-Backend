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

public sealed class AddressesPersistenceTests
{
    private const string AccountsMigration = "20260925062058_AddAccounts";

    [PostgreSqlFact]
    public async Task Migration_CreatesAddressesTableAndRequiredAccountForeignKey()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                to_regclass('accounts.addresses')::text,
                (SELECT is_nullable FROM information_schema.columns
                    WHERE table_schema = 'accounts'
                      AND table_name = 'accounts'
                      AND column_name = 'address_id'),
                (SELECT count(*) FROM pg_constraint
                    WHERE conrelid = 'accounts.addresses'::regclass),
                (SELECT count(*) FROM pg_indexes
                    WHERE schemaname = 'accounts' AND tablename = 'addresses'),
                (SELECT count(*) FROM pg_constraint
                    WHERE conrelid = 'accounts.accounts'::regclass
                      AND conname = 'fk_accounts_addresses_address_id')
            """;
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("accounts.addresses", reader.GetString(0));
        Assert.Equal("NO", reader.GetString(1));
        Assert.Equal(1, reader.GetInt64(2));
        Assert.Equal(2, reader.GetInt64(3));
        Assert.Equal(1, reader.GetInt64(4));
    }

    [PostgreSqlFact]
    public async Task Repository_LoadsNormalizedAddressById()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var address = Address.Create(
            new AddressId(Guid.NewGuid()),
            "  Бишкек  ",
            "  Токтогула   проспект  ",
            "  125  ",
            " ",
            " 42 ").Value;
        await using (var writeContext = database.CreateContext())
        {
            writeContext.Addresses.Add(address);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = database.CreateContext();
        var repository = new AddressRepository(readContext);

        var loaded = await repository.GetByIdAsync(address.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("Бишкек", loaded.Locality);
        Assert.Equal("Токтогула проспект", loaded.Street);
        Assert.Equal("125", loaded.House);
        Assert.Null(loaded.Building);
        Assert.Equal("42", loaded.Apartment);
        Assert.Equal("БИШКЕК ТОКТОГУЛА ПРОСПЕКТ 125 42", loaded.SearchText);
    }

    [PostgreSqlFact]
    public async Task Repository_UnknownId_ReturnsNull()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new AddressRepository(context);

        var loaded = await repository.GetByIdAsync(
            new AddressId(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(loaded);
    }

    [PostgreSqlFact]
    public async Task MissingAddress_IsRejectedByForeignKey()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var userAccount = CreateResidentIdentity("AB-123");
        var resident = CreateResident(userAccount);
        var account = Account.Create(
            new AccountId(Guid.NewGuid()),
            resident.Id,
            new AddressId(Guid.NewGuid()),
            "AB-123",
            DateTimeOffset.UtcNow).Value;
        await using var context = database.CreateContext();
        context.UserAccounts.Add(userAccount);
        await context.SaveChangesAsync();
        context.Residents.Add(resident);
        await context.SaveChangesAsync();
        context.Accounts.Add(account);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task ReferencedAddress_CannotBeDeleted()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var userAccount = CreateResidentIdentity("AB-123");
        var resident = CreateResident(userAccount);
        var address = CreateAddress();
        var account = Account.Create(
            new AccountId(Guid.NewGuid()),
            resident.Id,
            address.Id,
            "AB-123",
            DateTimeOffset.UtcNow).Value;
        await using var context = database.CreateContext();
        context.UserAccounts.Add(userAccount);
        await context.SaveChangesAsync();
        context.Residents.Add(resident);
        context.Addresses.Add(address);
        await context.SaveChangesAsync();
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        context.Addresses.Remove(address);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task Repository_ForwardsCanceledToken()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new AddressRepository(context);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByIdAsync(
                new AddressId(Guid.NewGuid()),
                cancellationTokenSource.Token));
    }

    [PostgreSqlFact]
    public async Task Migration_CanRollbackAddressesAndReapply()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(AccountsMigration);
        Assert.Null(await FindTableAsync(database.ConnectionString, "accounts.addresses"));
        Assert.Null(await FindColumnAsync(
            database.ConnectionString,
            "accounts",
            "accounts",
            "address_id"));

        await migrator.MigrateAsync();
        Assert.Equal(
            "accounts.addresses",
            await FindTableAsync(database.ConnectionString, "accounts.addresses"));
        Assert.Equal(
            "address_id",
            await FindColumnAsync(
                database.ConnectionString,
                "accounts",
                "accounts",
                "address_id"));
    }

    [PostgreSqlFact]
    public async Task Migration_WithExistingAccounts_FailsInsteadOfCreatingPlaceholderAddress()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(AccountsMigration);
        await InsertLegacyAccountAsync(database.ConnectionString);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => migrator.MigrateAsync());

        Assert.Equal(PostgresErrorCodes.RaiseException, exception.SqlState);
        Assert.Contains("no address backfill rule is approved", exception.MessageText);
        Assert.Null(await FindTableAsync(database.ConnectionString, "accounts.addresses"));
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

    private static async Task InsertLegacyAccountAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO identity.user_accounts
                (id, normalized_login, login_type, password_hash, role, created_at)
            VALUES
                (@user_id, 'LEGACY-1', 1, 'stored-password-hash', 1, @created_at);

            INSERT INTO residents.residents
                (id, user_id, full_name, created_at)
            VALUES
                (@resident_id, @user_id, 'Legacy Resident', @created_at);

            INSERT INTO accounts.accounts
                (id, resident_id, account_number, created_at)
            VALUES
                (@account_id, @resident_id, 'LEGACY-1', @created_at);
            """;
        command.Parameters.AddWithValue("user_id", Guid.NewGuid());
        command.Parameters.AddWithValue("resident_id", Guid.NewGuid());
        command.Parameters.AddWithValue("account_id", Guid.NewGuid());
        command.Parameters.AddWithValue("created_at", DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string?> FindTableAsync(
        string connectionString,
        string table)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass(@table)::text";
        command.Parameters.AddWithValue("table", table);
        return await command.ExecuteScalarAsync() as string;
    }

    private static async Task<string?> FindColumnAsync(
        string connectionString,
        string schema,
        string table,
        string column)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = @schema
              AND table_name = @table
              AND column_name = @column
            """;
        command.Parameters.AddWithValue("schema", schema);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);
        return await command.ExecuteScalarAsync() as string;
    }
}
