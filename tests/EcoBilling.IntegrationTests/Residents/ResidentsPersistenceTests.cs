using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace EcoBilling.IntegrationTests.Residents;

public sealed class ResidentsPersistenceTests
{
    private const string InitialIdentityMigration =
        "20260924115426_InitialIdentityPersistence";

    [PostgreSqlFact]
    public async Task Migration_CreatesResidentsSchemaTableAndConstraints()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                to_regnamespace('residents')::text,
                to_regclass('residents.residents')::text,
                (SELECT count(*) FROM pg_constraint
                    WHERE conrelid = 'residents.residents'::regclass),
                (SELECT count(*) FROM pg_indexes
                    WHERE schemaname = 'residents' AND tablename = 'residents')
            """;
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("residents", reader.GetString(0));
        Assert.Equal("residents.residents", reader.GetString(1));
        Assert.Equal(2, reader.GetInt64(2));
        Assert.Equal(2, reader.GetInt64(3));
    }

    [PostgreSqlFact]
    public async Task Repository_LoadsResidentByUserId()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var account = CreateResidentAccount("resident-1");
        var resident = CreateResident(account, "  Ada Lovelace  ");
        await SaveAsync(database, account, resident);
        await using var context = database.CreateContext();
        var repository = new ResidentRepository(context);

        var loaded = await repository.GetByUserIdAsync(account.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(resident.Id, loaded.Id);
        Assert.Equal(account.Id, loaded.UserId);
        Assert.Equal("Ada Lovelace", loaded.FullName);
    }

    [PostgreSqlFact]
    public async Task Repository_UnknownUserId_ReturnsNull()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new ResidentRepository(context);

        var loaded = await repository.GetByUserIdAsync(
            new UserId(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(loaded);
    }

    [PostgreSqlFact]
    public async Task DuplicateUserId_IsRejectedByDatabase()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var account = CreateResidentAccount("resident-duplicate");
        await using var context = database.CreateContext();
        context.UserAccounts.Add(account);
        context.Residents.Add(CreateResident(account, "Ada Lovelace"));
        context.Residents.Add(CreateResident(account, "Grace Hopper"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task MissingUserAccount_IsRejectedByForeignKey()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var account = CreateResidentAccount("resident-missing-account");
        var resident = CreateResident(account, "Ada Lovelace");
        await using var context = database.CreateContext();
        context.Residents.Add(resident);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task Repository_ForwardsCanceledToken()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new ResidentRepository(context);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByUserIdAsync(
                new UserId(Guid.NewGuid()),
                cancellationTokenSource.Token));
    }

    [PostgreSqlFact]
    public async Task Migration_CanRollbackResidentsAndReapply()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(InitialIdentityMigration);
        Assert.Null(await FindSchemaAsync(database.ConnectionString, "residents"));
        Assert.Equal("identity", await FindSchemaAsync(database.ConnectionString, "identity"));

        await migrator.MigrateAsync();
        Assert.Equal("residents", await FindSchemaAsync(database.ConnectionString, "residents"));
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
        UserAccount account,
        Resident resident)
    {
        await using var context = database.CreateContext();
        context.UserAccounts.Add(account);
        await context.SaveChangesAsync();
        context.Residents.Add(resident);
        await context.SaveChangesAsync();
    }

    private static UserAccount CreateResidentAccount(string accountNumber)
    {
        var loginIdentity = LoginIdentity.Create(LoginType.AccountNumber, accountNumber).Value;
        return UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity,
            "stored-password-hash",
            UserRole.Resident,
            DateTimeOffset.UtcNow).Value;
    }

    private static Resident CreateResident(UserAccount account, string fullName) =>
        Resident.Create(
            new ResidentId(Guid.NewGuid()),
            account,
            fullName,
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
