using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace EcoBilling.IntegrationTests.Controllers;

public sealed class ControllersPersistenceTests
{
    private const string ResidentsMigration =
        "20260925054603_AddResidentsProfile";

    [PostgreSqlFact]
    public async Task Migration_CreatesControllersSchemaTableAndConstraints()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                to_regnamespace('controllers')::text,
                to_regclass('controllers.controllers')::text,
                (SELECT count(*) FROM pg_constraint
                    WHERE conrelid = 'controllers.controllers'::regclass),
                (SELECT count(*) FROM pg_indexes
                    WHERE schemaname = 'controllers' AND tablename = 'controllers')
            """;
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("controllers", reader.GetString(0));
        Assert.Equal("controllers.controllers", reader.GetString(1));
        Assert.Equal(2, reader.GetInt64(2));
        Assert.Equal(2, reader.GetInt64(3));
    }

    [PostgreSqlFact]
    public async Task Repository_LoadsControllerByUserId()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var account = CreateControllerAccount("agent-1@example.com");
        var controller = CreateController(account, "  Grace Hopper  ");
        await SaveAsync(database, account, controller);
        await using var context = database.CreateContext();
        var repository = new ControllerRepository(context);

        var loaded = await repository.GetByUserIdAsync(account.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(controller.Id, loaded.Id);
        Assert.Equal(account.Id, loaded.UserId);
        Assert.Equal("Grace Hopper", loaded.FullName);
    }

    [PostgreSqlFact]
    public async Task Repository_UnknownUserId_ReturnsNull()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new ControllerRepository(context);

        var loaded = await repository.GetByUserIdAsync(
            new UserId(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(loaded);
    }

    [PostgreSqlFact]
    public async Task DuplicateUserId_IsRejectedByDatabase()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var account = CreateControllerAccount("duplicate@example.com");
        await using var context = database.CreateContext();
        context.UserAccounts.Add(account);
        context.Controllers.Add(CreateController(account, "Grace Hopper"));
        context.Controllers.Add(CreateController(account, "Katherine Johnson"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task MissingUserAccount_IsRejectedByForeignKey()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var account = CreateControllerAccount("missing@example.com");
        var controller = CreateController(account, "Grace Hopper");
        await using var context = database.CreateContext();
        context.Controllers.Add(controller);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task Repository_ForwardsCanceledToken()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new ControllerRepository(context);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByUserIdAsync(
                new UserId(Guid.NewGuid()),
                cancellationTokenSource.Token));
    }

    [PostgreSqlFact]
    public async Task Migration_CanRollbackControllersAndReapply()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(ResidentsMigration);
        Assert.Null(await FindSchemaAsync(database.ConnectionString, "controllers"));
        Assert.Equal("residents", await FindSchemaAsync(database.ConnectionString, "residents"));

        await migrator.MigrateAsync();
        Assert.Equal("controllers", await FindSchemaAsync(database.ConnectionString, "controllers"));
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
        Controller controller)
    {
        await using var context = database.CreateContext();
        context.UserAccounts.Add(account);
        await context.SaveChangesAsync();
        context.Controllers.Add(controller);
        await context.SaveChangesAsync();
    }

    private static UserAccount CreateControllerAccount(string email)
    {
        var loginIdentity = LoginIdentity.Create(LoginType.Email, email).Value;
        return UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity,
            "stored-password-hash",
            UserRole.Controller,
            DateTimeOffset.UtcNow).Value;
    }

    private static Controller CreateController(UserAccount account, string fullName) =>
        Controller.Create(
            new ControllerId(Guid.NewGuid()),
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
