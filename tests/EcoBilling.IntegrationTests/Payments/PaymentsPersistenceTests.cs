using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Payments.Domain;
using EcoBilling.Modules.Residents.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace EcoBilling.IntegrationTests.Payments;

public sealed class PaymentsPersistenceTests
{
    private const string BillingMigration = "20260925163302_AddBilling";

    [PostgreSqlFact]
    public async Task Migration_CreatesPaymentsSchemaTableConstraintsAndIndexes()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                to_regnamespace('payments')::text,
                to_regclass('payments.payments')::text,
                (SELECT count(*) FROM pg_constraint
                    WHERE conrelid = 'payments.payments'::regclass),
                (SELECT count(*) FROM pg_indexes
                    WHERE schemaname = 'payments' AND tablename = 'payments'),
                (SELECT count(*) FROM information_schema.columns
                    WHERE table_schema = 'payments'
                      AND table_name = 'payments'
                      AND is_nullable = 'NO'),
                (SELECT data_type FROM information_schema.columns
                    WHERE table_schema = 'payments'
                      AND table_name = 'payments'
                      AND column_name = 'amount')
            """;
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("payments", reader.GetString(0));
        Assert.Equal("payments.payments", reader.GetString(1));
        Assert.Equal(3, reader.GetInt64(2));
        Assert.Equal(3, reader.GetInt64(3));
        Assert.Equal(6, reader.GetInt64(4));
        Assert.Equal("numeric", reader.GetString(5));
    }

    [PostgreSqlFact]
    public async Task Repository_LoadsPaymentByIdWithExactValuesAndUtcTimes()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var accountGraph = CreateAccountGraph("AB-123");
        var paidAt = new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.FromHours(6));
        var createdAt = new DateTimeOffset(2026, 9, 25, 10, 1, 0, TimeSpan.FromHours(6));
        var payment = Payment.Create(
            new PaymentId(Guid.NewGuid()),
            accountGraph.Account.Id,
            123456789.123456789m,
            " Payment-Request-AbC-123 ",
            paidAt,
            createdAt).Value;
        await SaveAsync(database, accountGraph, payment);
        await using var context = database.CreateContext();
        var repository = new PaymentRepository(context);

        var loaded = await repository.GetByIdAsync(payment.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(accountGraph.Account.Id, loaded.AccountId);
        Assert.Equal(123456789.123456789m, loaded.Amount.Value);
        Assert.Equal(" Payment-Request-AbC-123 ", loaded.IdempotencyKey.Value);
        Assert.Equal(paidAt.ToUniversalTime(), loaded.PaidAt);
        Assert.Equal(createdAt.ToUniversalTime(), loaded.CreatedAt);
    }

    [PostgreSqlFact]
    public async Task Repository_UnknownId_ReturnsNull()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new PaymentRepository(context);

        var loaded = await repository.GetByIdAsync(
            new PaymentId(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(loaded);
    }

    [PostgreSqlFact]
    public async Task MissingAccount_IsRejectedByForeignKey()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var payment = CreatePayment(
            new AccountId(Guid.NewGuid()),
            "payment-request-123");
        await using var context = database.CreateContext();
        context.Payments.Add(payment);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task NonPositiveAmount_IsRejectedByDatabaseCheckConstraint()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var accountGraph = CreateAccountGraph("AB-123");
        await SaveAccountGraphAsync(database, accountGraph);
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO payments.payments
                (id, account_id, amount, idempotency_key, paid_at, created_at)
            VALUES
                (@id, @account_id, @amount, @idempotency_key, @paid_at, @created_at)
            """;
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("account_id", accountGraph.Account.Id.Value);
        command.Parameters.AddWithValue("amount", 0m);
        command.Parameters.AddWithValue("idempotency_key", "payment-request-zero");
        command.Parameters.AddWithValue("paid_at", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("created_at", DateTimeOffset.UtcNow);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("ck_payments_amount_positive", exception.ConstraintName);
    }

    [PostgreSqlFact]
    public async Task DuplicateIdempotencyKey_ForSameAccount_IsRejected()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var accountGraph = CreateAccountGraph("AB-123");
        await SaveAccountGraphAsync(database, accountGraph);
        await using var context = database.CreateContext();
        context.Payments.Add(
            CreatePayment(accountGraph.Account.Id, "payment-request-123"));
        context.Payments.Add(
            CreatePayment(accountGraph.Account.Id, "payment-request-123"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
        Assert.Equal("ux_payments_idempotency_key", postgresException.ConstraintName);
    }

    [PostgreSqlFact]
    public async Task DuplicateIdempotencyKey_AcrossAccounts_IsRejected()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var firstAccount = CreateAccountGraph("AB-123");
        var secondAccount = CreateAccountGraph("CD-456");
        await SaveAccountGraphAsync(database, firstAccount);
        await SaveAccountGraphAsync(database, secondAccount);
        await using var context = database.CreateContext();
        context.Payments.Add(
            CreatePayment(firstAccount.Account.Id, "payment-request-123"));
        context.Payments.Add(
            CreatePayment(secondAccount.Account.Id, "payment-request-123"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
        Assert.Equal("ux_payments_idempotency_key", postgresException.ConstraintName);
    }

    [PostgreSqlFact]
    public async Task CaseDistinctOpaqueIdempotencyKeys_AreAllowed()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var accountGraph = CreateAccountGraph("AB-123");
        await SaveAccountGraphAsync(database, accountGraph);
        await using var context = database.CreateContext();
        context.Payments.Add(
            CreatePayment(accountGraph.Account.Id, "payment-request-abc"));
        context.Payments.Add(
            CreatePayment(accountGraph.Account.Id, "PAYMENT-REQUEST-ABC"));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Payments.CountAsync());
    }

    [PostgreSqlFact]
    public async Task ReferencedAccount_CannotBeDeleted()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var accountGraph = CreateAccountGraph("AB-123");
        var payment = CreatePayment(
            accountGraph.Account.Id,
            "payment-request-123");
        await SaveAsync(database, accountGraph, payment);
        await using var context = database.CreateContext();
        context.Accounts.Remove(accountGraph.Account);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task Repository_ForwardsCanceledToken()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var repository = new PaymentRepository(context);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByIdAsync(
                new PaymentId(Guid.NewGuid()),
                cancellationTokenSource.Token));
    }

    [PostgreSqlFact]
    public async Task Migration_CanRollbackPaymentsAndReapply()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(BillingMigration);
        Assert.Null(await FindSchemaAsync(database.ConnectionString, "payments"));
        Assert.Equal("billing", await FindSchemaAsync(database.ConnectionString, "billing"));

        await migrator.MigrateAsync();
        Assert.Equal("payments", await FindSchemaAsync(database.ConnectionString, "payments"));
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

    private static Payment CreatePayment(
        AccountId accountId,
        string idempotencyKey) =>
        Payment.Create(
            new PaymentId(Guid.NewGuid()),
            accountId,
            100m,
            idempotencyKey,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow).Value;

    private static async Task SaveAsync(
        PostgreSqlTestDatabase database,
        AccountGraph accountGraph,
        Payment payment)
    {
        await SaveAccountGraphAsync(database, accountGraph);
        await using var context = database.CreateContext();
        context.Payments.Add(payment);
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
