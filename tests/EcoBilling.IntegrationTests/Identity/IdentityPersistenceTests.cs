using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace EcoBilling.IntegrationTests.Identity;

public sealed class IdentityPersistenceTests
{
    [PostgreSqlFact]
    public async Task Migration_AppliesToEmptyDatabase()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        Assert.Null(await FindUserAccountsTableAsync(database.ConnectionString));
        Assert.Null(await FindIdentitySchemaAsync(database.ConnectionString));

        await context.Database.MigrateAsync();
        await context.Database.MigrateAsync();

        Assert.Equal("identity", await FindIdentitySchemaAsync(database.ConnectionString));
        Assert.Equal(
            "identity.user_accounts",
            await FindUserAccountsTableAsync(database.ConnectionString));
    }

    [PostgreSqlFact]
    public async Task DbContext_SavesUserAccount()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var account = CreateAccount(UserRole.Resident, LoginType.AccountNumber, " A-100 ");

        context.UserAccounts.Add(account);
        await context.SaveChangesAsync();

        Assert.Equal(1, await context.UserAccounts.CountAsync());
    }

    [PostgreSqlFact]
    public async Task Repository_LoadsAccountByNormalizedLogin()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var account = CreateAccount(UserRole.Controller, LoginType.Email, " Agent@Example.com ");
        await SaveAsync(database, account);
        await using var context = database.CreateContext();
        var repository = new UserAccountRepository(context);
        var login = LoginIdentity.Create(LoginType.Email, "agent@example.COM").Value;

        var loaded = await repository.GetByLoginAsync(login, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(account.Id, loaded.Id);
        Assert.Equal("AGENT@EXAMPLE.COM", loaded.LoginIdentity.NormalizedValue);
    }

    [PostgreSqlFact]
    public async Task Repository_UnknownLogin_ReturnsNull()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new UserAccountRepository(context);
        var login = LoginIdentity.Create(LoginType.Email, "missing@example.com").Value;

        var loaded = await repository.GetByLoginAsync(login, CancellationToken.None);

        Assert.Null(loaded);
    }

    [PostgreSqlFact]
    public Task Resident_RoundTrips() =>
        AssertRoleRoundTripsAsync(UserRole.Resident, LoginType.AccountNumber, "resident-1");

    [PostgreSqlFact]
    public Task Controller_RoundTrips() =>
        AssertRoleRoundTripsAsync(UserRole.Controller, LoginType.Email, "controller@example.com");

    [PostgreSqlFact]
    public Task Director_RoundTrips() =>
        AssertRoleRoundTripsAsync(UserRole.Director, LoginType.Email, "director@example.com");

    [PostgreSqlFact]
    public async Task AccountNumber_IsTrimmedAndNormalizedCaseInsensitively()
    {
        await AssertNormalizedLoginAsync(
            UserRole.Resident,
            LoginType.AccountNumber,
            " account-AbC ",
            "ACCOUNT-ABC");
    }

    [PostgreSqlFact]
    public async Task Email_IsTrimmedAndNormalizedCaseInsensitively()
    {
        await AssertNormalizedLoginAsync(
            UserRole.Controller,
            LoginType.Email,
            " Person@Example.Com ",
            "PERSON@EXAMPLE.COM");
    }

    [PostgreSqlFact]
    public async Task DuplicateLoginTypeAndNormalizedLogin_IsRejected()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        context.UserAccounts.Add(CreateAccount(UserRole.Resident, LoginType.AccountNumber, "ab-1"));
        context.UserAccounts.Add(CreateAccount(UserRole.Resident, LoginType.AccountNumber, " AB-1 "));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task SameNormalizedLoginAcrossDifferentLoginTypes_IsAllowed()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        context.UserAccounts.Add(CreateAccount(UserRole.Resident, LoginType.AccountNumber, "same@example.com"));
        context.UserAccounts.Add(CreateAccount(UserRole.Controller, LoginType.Email, "same@example.com"));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.UserAccounts.CountAsync());
    }

    [PostgreSqlFact]
    public async Task InvalidLoginType_IsRejectedByDatabaseConstraint()
    {
        await AssertInvalidEnumValueIsRejectedAsync(loginType: 999, role: (int)UserRole.Resident);
    }

    [PostgreSqlFact]
    public async Task InvalidRole_IsRejectedByDatabaseConstraint()
    {
        await AssertInvalidEnumValueIsRejectedAsync(loginType: (int)LoginType.AccountNumber, role: 999);
    }

    [PostgreSqlFact]
    public async Task CreatedAt_RoundTripsAsUtc()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var original = new DateTimeOffset(2026, 9, 24, 17, 30, 0, TimeSpan.FromHours(6));
        var account = CreateAccount(
            UserRole.Resident,
            LoginType.AccountNumber,
            "utc-1",
            original);
        await SaveAsync(database, account);
        await using var context = database.CreateContext();

        var loaded = await context.UserAccounts.SingleAsync();

        Assert.Equal(TimeSpan.Zero, loaded.CreatedAt.Offset);
        Assert.Equal(original.UtcDateTime, loaded.CreatedAt.UtcDateTime);
    }

    [PostgreSqlFact]
    public async Task PasswordHash_IsPersistedWithoutPlaintext()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        const string Password = "never-store-this-password";
        var hash = new PasswordHasherAdapter().Hash(Password);
        var account = CreateAccount(
            UserRole.Resident,
            LoginType.AccountNumber,
            "hash-1",
            passwordHash: hash);
        await SaveAsync(database, account);
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT password_hash FROM identity.user_accounts";

        var stored = Assert.IsType<string>(await command.ExecuteScalarAsync());

        Assert.Equal(hash, stored);
        Assert.NotEqual(Password, stored);
        Assert.DoesNotContain(Password, stored, StringComparison.Ordinal);
    }

    [PostgreSqlFact]
    public async Task Repository_ForwardsCanceledToken()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new UserAccountRepository(context);
        var login = LoginIdentity.Create(LoginType.AccountNumber, "cancel-1").Value;
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByLoginAsync(login, cancellationTokenSource.Token));
    }

    [PostgreSqlFact]
    public async Task Migration_CanRollbackAndReapply()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(Migration.InitialDatabase);
        Assert.Null(await FindUserAccountsTableAsync(database.ConnectionString));
        Assert.Null(await FindIdentitySchemaAsync(database.ConnectionString));

        await migrator.MigrateAsync();
        Assert.Equal("identity", await FindIdentitySchemaAsync(database.ConnectionString));
        Assert.Equal(
            "identity.user_accounts",
            await FindUserAccountsTableAsync(database.ConnectionString));
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

    private static async Task SaveAsync(PostgreSqlTestDatabase database, UserAccount account)
    {
        await using var context = database.CreateContext();
        context.UserAccounts.Add(account);
        await context.SaveChangesAsync();
    }

    private static async Task AssertRoleRoundTripsAsync(
        UserRole role,
        LoginType loginType,
        string login)
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var account = CreateAccount(role, loginType, login);
        await SaveAsync(database, account);
        await using var context = database.CreateContext();
        var repository = new UserAccountRepository(context);
        var loginIdentity = LoginIdentity.Create(loginType, login).Value;

        var loaded = await repository.GetByLoginAsync(loginIdentity, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(role, loaded.Role);
        Assert.Equal(loginType, loaded.LoginIdentity.Type);
    }

    private static async Task AssertNormalizedLoginAsync(
        UserRole role,
        LoginType loginType,
        string login,
        string expected)
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var account = CreateAccount(role, loginType, login);
        await SaveAsync(database, account);
        await using var context = database.CreateContext();

        var loaded = await context.UserAccounts.SingleAsync();

        Assert.Equal(expected, loaded.LoginIdentity.NormalizedValue);
    }

    private static async Task AssertInvalidEnumValueIsRejectedAsync(int loginType, int role)
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();

        await Assert.ThrowsAsync<PostgresException>(
            () => context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO identity.user_accounts
                    (id, login_type, normalized_login, password_hash, role, created_at)
                VALUES
                    ({Guid.NewGuid()}, {loginType}, {"INVALID"}, {"hash"}, {role}, {DateTimeOffset.UtcNow})
                """));
    }

    private static UserAccount CreateAccount(
        UserRole role,
        LoginType loginType,
        string login,
        DateTimeOffset? createdAt = null,
        string passwordHash = "stored-password-hash")
    {
        var loginIdentity = LoginIdentity.Create(loginType, login).Value;
        return UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity,
            passwordHash,
            role,
            createdAt ?? DateTimeOffset.UtcNow).Value;
    }

    private static async Task<string?> FindUserAccountsTableAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('identity.user_accounts')::text";
        return await command.ExecuteScalarAsync() as string;
    }

    private static async Task<string?> FindIdentitySchemaAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regnamespace('identity')::text";
        return await command.ExecuteScalarAsync() as string;
    }
}
