using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Reports;

public sealed class ResidentSelfServiceReader(EcoBillingDbContext dbContext)
    : IResidentSelfServiceReader
{
    public async Task<ResidentAccountView?> GetAccountAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var resident = await dbContext.Residents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == userId,
                cancellationToken);
        if (resident is null)
        {
            return null;
        }

        var account = await dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.ResidentId == resident.Id,
                cancellationToken);
        if (account is null)
        {
            return null;
        }

        var address = await dbContext.Addresses
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == account.AddressId,
                cancellationToken);

        return new ResidentAccountView(
            resident.Id.Value,
            resident.FullName,
            account.Id.Value,
            account.Number.Value,
            address.Id.Value,
            address.Locality,
            address.Street,
            address.House,
            address.Building,
            address.Apartment);
    }

    public async Task<IReadOnlyList<ResidentMeterView>> ListMetersAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        var account = await GetAccountEntityAsync(userId, cancellationToken);
        if (account is null)
        {
            return [];
        }

        var meters = await dbContext.Meters
            .AsNoTracking()
            .Where(meter => meter.AccountId == account.Id)
            .OrderByDescending(meter => meter.IsActive)
            .ThenByDescending(meter => meter.InstalledAt)
            .ToListAsync(cancellationToken);

        return meters.Select(
                meter => new ResidentMeterView(
                    meter.Id.Value,
                    meter.SerialNumber.Value,
                    meter.InstalledAt,
                    meter.IsActive,
                    meter.RetiredAt))
            .ToArray();
    }

    public async Task<IReadOnlyList<ResidentReadingView>> ListReadingsAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        var account = await GetAccountEntityAsync(userId, cancellationToken);
        if (account is null)
        {
            return [];
        }

        var meters = await dbContext.Meters
            .AsNoTracking()
            .Where(meter => meter.AccountId == account.Id)
            .ToListAsync(cancellationToken);

        var result = new List<ResidentReadingView>();
        foreach (var meter in meters)
        {
            var readings = await dbContext.MeterReadings
                .AsNoTracking()
                .Where(reading => reading.MeterId == meter.Id)
                .OrderByDescending(reading => reading.MeasuredAt)
                .ThenByDescending(reading => reading.CreatedAt)
                .ToListAsync(cancellationToken);

            result.AddRange(
                readings.Select(
                    reading => new ResidentReadingView(
                        reading.Id.Value,
                        reading.MeterId.Value,
                        reading.Value.Value,
                        reading.MeasuredAt,
                        reading.Source.ToString())));
        }

        return result
            .OrderByDescending(item => item.MeasuredAt)
            .ToArray();
    }

    public async Task<IReadOnlyList<ResidentChargeView>> ListChargesAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        var account = await GetAccountEntityAsync(userId, cancellationToken);
        if (account is null)
        {
            return [];
        }

        var charges = await dbContext.Charges
            .AsNoTracking()
            .Where(charge => charge.AccountId == account.Id)
            .OrderByDescending(charge => charge.PeriodStart)
            .ToListAsync(cancellationToken);

        return charges.Select(
                charge => new ResidentChargeView(
                    charge.Id.Value,
                    charge.PeriodStart,
                    charge.PeriodEnd,
                    charge.Consumption,
                    charge.Amount,
                    charge.Currency,
                    charge.CalculationVersion))
            .ToArray();
    }

    public async Task<IReadOnlyList<ResidentPaymentView>> ListPaymentsAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        var account = await GetAccountEntityAsync(userId, cancellationToken);
        if (account is null)
        {
            return [];
        }

        var payments = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.AccountId == account.Id)
            .OrderByDescending(payment => payment.PaidAt)
            .ToListAsync(cancellationToken);

        return payments.Select(
                payment => new ResidentPaymentView(
                    payment.Id.Value,
                    payment.Amount.Value,
                    payment.PaidAt))
            .ToArray();
    }

    public async Task<ResidentFinancialView?> GetFinancialAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        var account = await GetAccountEntityAsync(userId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var charges = await dbContext.Charges
            .AsNoTracking()
            .Where(charge => charge.AccountId == account.Id)
            .ToListAsync(cancellationToken);
        var totalCharges = charges.Sum(charge => charge.Amount);

        var payments = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.AccountId == account.Id)
            .ToListAsync(cancellationToken);
        var totalPayments = payments.Sum(payment => payment.Amount.Value);

        var paymentIds = payments.Select(payment => payment.Id).ToArray();
        var allocated = paymentIds.Length == 0
            ? 0m
            : (await dbContext.PaymentAllocations
                .AsNoTracking()
                .Where(allocation => paymentIds.Contains(allocation.PaymentId))
                .ToListAsync(cancellationToken))
                .Sum(allocation => allocation.Amount);

        return new ResidentFinancialView(
            totalCharges,
            totalPayments,
            Math.Max(0m, totalCharges - allocated),
            account.Overpayment,
            "KGS");
    }

    private async Task<EcoBilling.Modules.Accounts.Domain.Account?> GetAccountEntityAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var resident = await dbContext.Residents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == userId,
                cancellationToken);
        if (resident is null)
        {
            return null;
        }

        return await dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                account => account.ResidentId == resident.Id,
                cancellationToken);
    }
}
