using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Contracts;
using EcoBilling.Modules.Residents.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Reports;

public sealed class ResidentSelfServiceReader(EcoBillingDbContext dbContext)
    : IResidentSelfServiceReader
{
    public Task<ResidentAccountView?> GetAccountAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        return (
            from resident in dbContext.Residents.AsNoTracking()
            join account in dbContext.Accounts.AsNoTracking()
                on resident.Id equals account.ResidentId
            join address in dbContext.Addresses.AsNoTracking()
                on account.AddressId equals address.Id
            where resident.UserId == userId
            select new ResidentAccountView(
                resident.Id.Value,
                resident.FullName,
                account.Id.Value,
                account.Number.Value,
                address.Id.Value,
                address.Locality,
                address.Street,
                address.House,
                address.Building,
                address.Apartment))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResidentMeterView>> ListMetersAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        return await (
            from resident in dbContext.Residents.AsNoTracking()
            join account in dbContext.Accounts.AsNoTracking()
                on resident.Id equals account.ResidentId
            join meter in dbContext.Meters.AsNoTracking()
                on account.Id equals meter.AccountId
            where resident.UserId == userId
            orderby meter.IsActive descending, meter.InstalledAt descending
            select new ResidentMeterView(
                meter.Id.Value,
                meter.SerialNumber.Value,
                meter.InstalledAt,
                meter.IsActive,
                meter.RetiredAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResidentReadingView>> ListReadingsAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        return await (
            from resident in dbContext.Residents.AsNoTracking()
            join account in dbContext.Accounts.AsNoTracking()
                on resident.Id equals account.ResidentId
            join meter in dbContext.Meters.AsNoTracking()
                on account.Id equals meter.AccountId
            join reading in dbContext.MeterReadings.AsNoTracking()
                on meter.Id equals reading.MeterId
            where resident.UserId == userId
            orderby reading.MeasuredAt descending, reading.CreatedAt descending
            select new ResidentReadingView(
                reading.Id.Value,
                reading.MeterId.Value,
                reading.Value.Value,
                reading.MeasuredAt,
                reading.Source.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResidentChargeView>> ListChargesAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        return await (
            from resident in dbContext.Residents.AsNoTracking()
            join account in dbContext.Accounts.AsNoTracking()
                on resident.Id equals account.ResidentId
            join charge in dbContext.Charges.AsNoTracking()
                on account.Id equals charge.AccountId
            where resident.UserId == userId
            orderby charge.PeriodStart descending
            select new ResidentChargeView(
                charge.Id.Value,
                charge.PeriodStart,
                charge.PeriodEnd,
                charge.Consumption,
                charge.Amount,
                charge.Currency,
                charge.CalculationVersion))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResidentPaymentView>> ListPaymentsAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        return await (
            from resident in dbContext.Residents.AsNoTracking()
            join account in dbContext.Accounts.AsNoTracking()
                on resident.Id equals account.ResidentId
            join payment in dbContext.Payments.AsNoTracking()
                on account.Id equals payment.AccountId
            where resident.UserId == userId
            orderby payment.PaidAt descending
            select new ResidentPaymentView(
                payment.Id.Value,
                payment.Amount.Value,
                payment.PaidAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ResidentFinancialView?> GetFinancialAsync(
        UserId userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userId);

        var accountId = await (
            from resident in dbContext.Residents.AsNoTracking()
            join account in dbContext.Accounts.AsNoTracking()
                on resident.Id equals account.ResidentId
            where resident.UserId == userId
            select (Guid?)account.Id.Value)
            .SingleOrDefaultAsync(cancellationToken);
        if (accountId is null)
        {
            return null;
        }

        var typedAccountId = new EcoBilling.Modules.Accounts.Domain.AccountId(
            accountId.Value);

        var totalCharges = await dbContext.Charges
            .AsNoTracking()
            .Where(charge => charge.AccountId == typedAccountId)
            .SumAsync(charge => (decimal?)charge.Amount, cancellationToken) ?? 0m;

        var payments = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.AccountId == typedAccountId)
            .ToListAsync(cancellationToken);
        var totalPayments = payments.Sum(payment => payment.Amount.Value);

        var allocated = await (
            from allocation in dbContext.PaymentAllocations.AsNoTracking()
            join payment in dbContext.Payments.AsNoTracking()
                on allocation.PaymentId equals payment.Id
            where payment.AccountId == typedAccountId
            select (decimal?)allocation.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        return new ResidentFinancialView(
            totalCharges,
            totalPayments,
            Math.Max(0m, totalCharges - allocated),
            Math.Max(0m, totalPayments - allocated),
            "KGS");
    }
}
