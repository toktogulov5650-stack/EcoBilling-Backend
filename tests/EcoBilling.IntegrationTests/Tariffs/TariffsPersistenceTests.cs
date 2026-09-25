using EcoBilling.Infrastructure.Persistence.Repositories;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Tariffs.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using NpgsqlTypes;

namespace EcoBilling.IntegrationTests.Tariffs;

public sealed class TariffsPersistenceTests
{
    private const string ReadingsMigration = "20260925113417_AddReadings";

    [PostgreSqlFact]
    public async Task Migration_CreatesTariffsSchemaTablesConstraintsAndExtension()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                to_regnamespace('tariffs')::text,
                to_regclass('tariffs.tariffs')::text,
                to_regclass('tariffs.tariff_versions')::text,
                (SELECT count(*) FROM pg_constraint
                    WHERE conrelid = 'tariffs.tariffs'::regclass),
                (SELECT count(*) FROM pg_constraint
                    WHERE conrelid = 'tariffs.tariff_versions'::regclass),
                (SELECT count(*) FROM pg_indexes
                    WHERE schemaname = 'tariffs' AND tablename = 'tariff_versions'),
                (SELECT count(*) FROM information_schema.columns
                    WHERE table_schema = 'tariffs'
                      AND table_name = 'tariff_versions'
                      AND is_nullable = 'NO'),
                (SELECT extname FROM pg_extension WHERE extname = 'btree_gist')
            """;
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("tariffs", reader.GetString(0));
        Assert.Equal("tariffs.tariffs", reader.GetString(1));
        Assert.Equal("tariffs.tariff_versions", reader.GetString(2));
        Assert.Equal(1, reader.GetInt64(3));
        Assert.Equal(5, reader.GetInt64(4));
        Assert.Equal(3, reader.GetInt64(5));
        Assert.Equal(5, reader.GetInt64(6));
        Assert.Equal("btree_gist", reader.GetString(7));
    }

    [PostgreSqlFact]
    public async Task TariffRepository_LoadsTariffById()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var createdAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(6));
        var tariff = Tariff.Create(
            new TariffId(Guid.NewGuid()),
            "  Население   базовый ",
            createdAt).Value;
        await SaveTariffAsync(database, tariff);
        await using var context = database.CreateContext();
        var repository = new TariffRepository(context);

        var loaded = await repository.GetByIdAsync(tariff.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("Население базовый", loaded.Name.Value);
        Assert.Equal(createdAt.ToUniversalTime(), loaded.CreatedAt);
    }

    [PostgreSqlFact]
    public async Task TariffVersionRepository_LoadsVersionByIdWithExactRate()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var tariff = CreateTariff("Население");
        var createdAt = new DateTimeOffset(2025, 12, 20, 10, 0, 0, TimeSpan.FromHours(6));
        var version = TariffVersion.Create(
            new TariffVersionId(Guid.NewGuid()),
            tariff.Id,
            123456789.123456789m,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 7, 1),
            createdAt).Value;
        await SaveAsync(database, tariff, version);
        await using var context = database.CreateContext();
        var repository = new TariffVersionRepository(context);

        var loaded = await repository.GetByIdAsync(version.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(tariff.Id, loaded.TariffId);
        Assert.Equal(123456789.123456789m, loaded.Rate.Value);
        Assert.Equal(new DateOnly(2026, 1, 1), loaded.EffectiveFrom);
        Assert.Equal(new DateOnly(2026, 7, 1), loaded.EffectiveTo);
        Assert.Equal(createdAt.ToUniversalTime(), loaded.CreatedAt);
    }

    [PostgreSqlFact]
    public async Task Repositories_UnknownIds_ReturnNull()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var tariffRepository = new TariffRepository(context);
        var versionRepository = new TariffVersionRepository(context);

        var tariff = await tariffRepository.GetByIdAsync(
            new TariffId(Guid.NewGuid()),
            CancellationToken.None);
        var version = await versionRepository.GetByIdAsync(
            new TariffVersionId(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(tariff);
        Assert.Null(version);
    }

    [PostgreSqlFact]
    public async Task MissingTariff_IsRejectedByForeignKey()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var version = CreateVersion(
            new TariffId(Guid.NewGuid()),
            12m,
            new DateOnly(2026, 1, 1),
            null);
        await using var context = database.CreateContext();
        context.TariffVersions.Add(version);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task NegativeRate_IsRejectedByDatabaseCheckConstraint()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var tariff = CreateTariff("Население");
        await SaveTariffAsync(database, tariff);

        var exception = await InsertRawVersionExpectingFailureAsync(
            database.ConnectionString,
            tariff.Id,
            -1m,
            new DateOnly(2026, 1, 1),
            null);

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("ck_tariff_versions_rate_non_negative", exception.ConstraintName);
    }

    [PostgreSqlFact]
    public async Task InvalidPeriod_IsRejectedByDatabaseCheckConstraint()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var tariff = CreateTariff("Население");
        await SaveTariffAsync(database, tariff);

        var exception = await InsertRawVersionExpectingFailureAsync(
            database.ConnectionString,
            tariff.Id,
            12m,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("ck_tariff_versions_effective_period", exception.ConstraintName);
    }

    [PostgreSqlFact]
    public async Task OverlappingPeriods_ForSameTariff_AreRejected()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var tariff = CreateTariff("Население");
        var firstVersion = CreateVersion(
            tariff.Id,
            12m,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 7, 1));
        await SaveAsync(database, tariff, firstVersion);
        await using var context = database.CreateContext();
        context.TariffVersions.Add(
            CreateVersion(
                tariff.Id,
                13m,
                new DateOnly(2026, 6, 1),
                new DateOnly(2027, 1, 1)));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(PostgresErrorCodes.ExclusionViolation, postgresException.SqlState);
        Assert.Equal(
            "ex_tariff_versions_tariff_id_effective_period",
            postgresException.ConstraintName);
    }

    [PostgreSqlFact]
    public async Task AdjacentPeriods_ForSameTariff_AreAllowed()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var tariff = CreateTariff("Население");
        await SaveTariffAsync(database, tariff);
        await using var context = database.CreateContext();
        context.TariffVersions.Add(
            CreateVersion(
                tariff.Id,
                12m,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 7, 1)));
        context.TariffVersions.Add(
            CreateVersion(
                tariff.Id,
                13m,
                new DateOnly(2026, 7, 1),
                null));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.TariffVersions.CountAsync());
    }

    [PostgreSqlFact]
    public async Task OverlappingPeriods_ForDifferentTariffs_AreAllowed()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var residential = CreateTariff("Население");
        var commercial = CreateTariff("Коммерческий");
        await using (var context = database.CreateContext())
        {
            context.Tariffs.AddRange(residential, commercial);
            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            context.TariffVersions.Add(
                CreateVersion(
                    residential.Id,
                    12m,
                    new DateOnly(2026, 1, 1),
                    null));
            context.TariffVersions.Add(
                CreateVersion(
                    commercial.Id,
                    20m,
                    new DateOnly(2026, 1, 1),
                    null));

            await context.SaveChangesAsync();
        }

        await using var verificationContext = database.CreateContext();
        Assert.Equal(2, await verificationContext.TariffVersions.CountAsync());
    }

    [PostgreSqlFact]
    public async Task ReferencedTariff_CannotBeDeleted()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        var tariff = CreateTariff("Население");
        var version = CreateVersion(
            tariff.Id,
            12m,
            new DateOnly(2026, 1, 1),
            null);
        await SaveAsync(database, tariff, version);
        await using var context = database.CreateContext();
        context.Tariffs.Remove(tariff);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlFact]
    public async Task Repositories_ForwardCanceledToken()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var tariffRepository = new TariffRepository(context);
        var versionRepository = new TariffVersionRepository(context);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => tariffRepository.GetByIdAsync(
                new TariffId(Guid.NewGuid()),
                cancellationTokenSource.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => versionRepository.GetByIdAsync(
                new TariffVersionId(Guid.NewGuid()),
                cancellationTokenSource.Token));
    }

    [PostgreSqlFact]
    public async Task Migration_CanRollbackTariffsAndReapply()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(ReadingsMigration);
        Assert.Null(await FindSchemaAsync(database.ConnectionString, "tariffs"));
        Assert.Equal("readings", await FindSchemaAsync(database.ConnectionString, "readings"));
        Assert.Equal(
            "btree_gist",
            await FindExtensionAsync(database.ConnectionString, "btree_gist"));

        await migrator.MigrateAsync();
        Assert.Equal("tariffs", await FindSchemaAsync(database.ConnectionString, "tariffs"));
        Assert.Equal(
            "btree_gist",
            await FindExtensionAsync(database.ConnectionString, "btree_gist"));
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

    private static Tariff CreateTariff(string name) =>
        Tariff.Create(
            new TariffId(Guid.NewGuid()),
            name,
            DateTimeOffset.UtcNow).Value;

    private static TariffVersion CreateVersion(
        TariffId tariffId,
        decimal rate,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo) =>
        TariffVersion.Create(
            new TariffVersionId(Guid.NewGuid()),
            tariffId,
            rate,
            effectiveFrom,
            effectiveTo,
            DateTimeOffset.UtcNow).Value;

    private static async Task SaveTariffAsync(
        PostgreSqlTestDatabase database,
        Tariff tariff)
    {
        await using var context = database.CreateContext();
        context.Tariffs.Add(tariff);
        await context.SaveChangesAsync();
    }

    private static async Task SaveAsync(
        PostgreSqlTestDatabase database,
        Tariff tariff,
        TariffVersion version)
    {
        await SaveTariffAsync(database, tariff);
        await using var context = database.CreateContext();
        context.TariffVersions.Add(version);
        await context.SaveChangesAsync();
    }

    private static async Task<PostgresException> InsertRawVersionExpectingFailureAsync(
        string connectionString,
        TariffId tariffId,
        decimal rate,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO tariffs.tariff_versions
                (id, tariff_id, rate, effective_from, effective_to, created_at)
            VALUES
                (@id, @tariff_id, @rate, @effective_from, @effective_to, @created_at)
            """;
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("tariff_id", tariffId.Value);
        command.Parameters.AddWithValue("rate", rate);
        command.Parameters.AddWithValue("effective_from", effectiveFrom);
        command.Parameters.AddWithValue(
            "effective_to",
            NpgsqlDbType.Date,
            effectiveTo is null ? DBNull.Value : effectiveTo.Value);
        command.Parameters.AddWithValue("created_at", DateTimeOffset.UtcNow);

        return await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());
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

    private static async Task<string?> FindExtensionAsync(
        string connectionString,
        string extension)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT extname FROM pg_extension WHERE extname = @extension";
        command.Parameters.AddWithValue("extension", extension);
        return await command.ExecuteScalarAsync() as string;
    }
}
