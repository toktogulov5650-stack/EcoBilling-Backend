using EcoBilling.Modules.Reports.Contracts;
using EcoBilling.Modules.Reports.Features.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.Infrastructure.Persistence.Reports;

public sealed class DistrictFinancialSummaryReader(EcoBillingDbContext dbContext)
    : IDistrictFinancialSummaryReader
{
    public async Task<DistrictFinancialSummary> ReadAsync(
        CancellationToken cancellationToken)
    {
        var totalCharges = await dbContext.Charges
            .AsNoTracking()
            .SumAsync(charge => (decimal?)charge.Amount, cancellationToken) ?? 0m;

        var payments = await dbContext.Payments
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var totalPayments = payments.Sum(payment => payment.Amount.Value);

        var allocated = await dbContext.PaymentAllocations
            .AsNoTracking()
            .SumAsync(allocation => (decimal?)allocation.Amount, cancellationToken) ?? 0m;

        var consumption = await dbContext.Charges
            .AsNoTracking()
            .SumAsync(charge => (decimal?)charge.Consumption, cancellationToken) ?? 0m;

        var controllers = await dbContext.Controllers
            .AsNoTracking()
            .OrderBy(controller => controller.FullName)
            .ToListAsync(cancellationToken);

        var performance = new List<ControllerPerformanceSummary>(controllers.Count);
        foreach (var controller in controllers)
        {
            var assignedAddresses = await dbContext.ControllerAssignments
                .AsNoTracking()
                .CountAsync(
                    assignment => assignment.ControllerId == controller.Id,
                    cancellationToken);

            var readingsEntered = await dbContext.MeterReadings
                .AsNoTracking()
                .LongCountAsync(
                    reading => reading.AuthorUserId == controller.UserId,
                    cancellationToken);

            performance.Add(
                new ControllerPerformanceSummary(
                    controller.Id.Value,
                    controller.FullName,
                    assignedAddresses,
                    readingsEntered));
        }

        return new DistrictFinancialSummary(
            totalCharges,
            totalPayments,
            Math.Max(0m, totalCharges - allocated),
            Math.Max(0m, totalPayments - allocated),
            consumption,
            "KGS",
            performance);
    }
}
