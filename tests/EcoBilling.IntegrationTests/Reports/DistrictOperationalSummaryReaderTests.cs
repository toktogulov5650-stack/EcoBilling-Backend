using EcoBilling.Infrastructure.Persistence.Reports;
using EcoBilling.IntegrationTests.Infrastructure;
using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Payments.Domain;
using EcoBilling.Modules.Readings.Domain;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Tariffs.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.IntegrationTests.Reports;

public sealed class DistrictOperationalSummaryReaderTests
{
    [PostgreSqlFact]
    public async Task ReadAsync_EmptyDatabase_ReturnsZeros()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var reader = new DistrictOperationalSummaryReader(context);

        var summary = await reader.ReadAsync(CancellationToken.None);

        Assert.Equal(0, summary.Residents);
        Assert.Equal(0, summary.Controllers);
        Assert.Equal(0, summary.Accounts);
        Assert.Equal(0, summary.Addresses);
        Assert.Equal(0, summary.Meters);
        Assert.Equal(0, summary.MeterReadings);
        Assert.Equal(0, summary.Tariffs);
        Assert.Equal(0, summary.TariffVersions);
        Assert.Equal(0, summary.Charges);
        Assert.Equal(0, summary.Payments);
    }

    [PostgreSqlFact]
    public async Task ReadAsync_PopulatedDatabase_ReturnsStoredEntityCounts()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await SeedOneOfEachAsync(database);
        await using var context = database.CreateContext();
        var reader = new DistrictOperationalSummaryReader(context);

        var summary = await reader.ReadAsync(CancellationToken.None);

        Assert.Equal(1, summary.Residents);
        Assert.Equal(1, summary.Controllers);
        Assert.Equal(1, summary.Accounts);
        Assert.Equal(1, summary.Addresses);
        Assert.Equal(1, summary.Meters);
        Assert.Equal(1, summary.MeterReadings);
        Assert.Equal(1, summary.Tariffs);
        Assert.Equal(1, summary.TariffVersions);
        Assert.Equal(1, summary.Charges);
        Assert.Equal(1, summary.Payments);
    }

    [PostgreSqlFact]
    public async Task ReadAsync_ForwardsCanceledToken()
    {
        await using var database = await CreateMigratedDatabaseAsync();
        await using var context = database.CreateContext();
        var reader = new DistrictOperationalSummaryReader(context);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => reader.ReadAsync(cancellationTokenSource.Token));
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

    private static async Task SeedOneOfEachAsync(PostgreSqlTestDatabase database)
    {
        var residentIdentity = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            LoginIdentity.Create(LoginType.AccountNumber, "AB-123").Value,
            "stored-password-hash",
            UserRole.Resident,
            DateTimeOffset.UtcNow).Value;
        var controllerIdentity = UserAccount.Create(
            new UserId(Guid.NewGuid()),
            LoginIdentity.Create(LoginType.Email, "controller@example.com").Value,
            "stored-password-hash",
            UserRole.Controller,
            DateTimeOffset.UtcNow).Value;
        var resident = Resident.Create(
            new ResidentId(Guid.NewGuid()),
            residentIdentity,
            "Ada Lovelace",
            DateTimeOffset.UtcNow).Value;
        var controller = Controller.Create(
            new ControllerId(Guid.NewGuid()),
            controllerIdentity,
            "Grace Hopper",
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
            "AB-123",
            DateTimeOffset.UtcNow).Value;
        var meter = Meter.Create(
            new MeterId(Guid.NewGuid()),
            account.Id,
            "SN-AB-123",
            DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow).Value;
        var reading = MeterReading.Create(
            new MeterReadingId(Guid.NewGuid()),
            meter.Id,
            123.456m,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow).Value;
        var tariff = Tariff.Create(
            new TariffId(Guid.NewGuid()),
            "Население",
            DateTimeOffset.UtcNow).Value;
        var tariffVersion = TariffVersion.Create(
            new TariffVersionId(Guid.NewGuid()),
            tariff.Id,
            12.345m,
            new DateOnly(2026, 1, 1),
            null,
            DateTimeOffset.UtcNow).Value;
        var charge = Charge.Create(
            new ChargeId(Guid.NewGuid()),
            account.Id,
            tariffVersion.Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1),
            100m,
            DateTimeOffset.UtcNow).Value;
        var payment = Payment.Create(
            new PaymentId(Guid.NewGuid()),
            account.Id,
            100m,
            "payment-request-report-test",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow).Value;

        await using var context = database.CreateContext();
        context.UserAccounts.AddRange(residentIdentity, controllerIdentity);
        await context.SaveChangesAsync();
        context.Residents.Add(resident);
        context.Controllers.Add(controller);
        context.Addresses.Add(address);
        await context.SaveChangesAsync();
        context.Accounts.Add(account);
        context.Tariffs.Add(tariff);
        await context.SaveChangesAsync();
        context.Meters.Add(meter);
        context.TariffVersions.Add(tariffVersion);
        await context.SaveChangesAsync();
        context.MeterReadings.Add(reading);
        context.Charges.Add(charge);
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
    }
}
