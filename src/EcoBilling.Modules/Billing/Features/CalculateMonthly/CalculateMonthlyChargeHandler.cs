using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Billing.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Billing.Features.CalculateMonthly;

public sealed class CalculateMonthlyChargeHandler(
    IBillingCalculationRepository repository)
{
    public async Task<Result<CalculateMonthlyChargeResult>> Handle(
        CalculateMonthlyChargeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.AccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        if (command.Year < 2000 || command.Year > 2200 ||
            command.Month is < 1 or > 12)
        {
            return Result<CalculateMonthlyChargeResult>.Failure(
                ChargeErrors.InvalidBillingPeriod);
        }

        var start = new DateOnly(command.Year, command.Month, 1);
        var end = start.AddMonths(1);

        var persistence = await repository.CalculateAsync(
            command.AccountId,
            start,
            end,
            command.ActorId,
            command.CorrelationId,
            cancellationToken);

        if (persistence.Charge is not null &&
            persistence.Outcome is BillingCalculationPersistenceOutcome.Calculated or
                BillingCalculationPersistenceOutcome.Replayed)
        {
            var charge = persistence.Charge;
            return Result<CalculateMonthlyChargeResult>.Success(
                new CalculateMonthlyChargeResult(
                    charge.Id.Value,
                    charge.AccountId.Value,
                    charge.TariffVersionId.Value,
                    charge.PeriodStart,
                    charge.PeriodEnd,
                    charge.Consumption,
                    charge.Amount,
                    charge.Currency,
                    charge.CalculationVersion,
                    persistence.IsReplay));
        }

        return persistence.Outcome switch
        {
            BillingCalculationPersistenceOutcome.AccountNotFound =>
                Result<CalculateMonthlyChargeResult>.Failure(
                    ChargeErrors.AccountNotFound),
            BillingCalculationPersistenceOutcome.MeterNotFound =>
                Result<CalculateMonthlyChargeResult>.Failure(
                    ChargeErrors.MeterNotFound),
            BillingCalculationPersistenceOutcome.MultipleMetersRequirePolicy =>
                Result<CalculateMonthlyChargeResult>.Failure(
                    ChargeErrors.MultipleMetersRequirePolicy),
            BillingCalculationPersistenceOutcome.PreviousReadingNotFound =>
                Result<CalculateMonthlyChargeResult>.Failure(
                    ChargeErrors.PreviousReadingNotFound),
            BillingCalculationPersistenceOutcome.CurrentReadingNotFound =>
                Result<CalculateMonthlyChargeResult>.Failure(
                    ChargeErrors.CurrentReadingNotFound),
            BillingCalculationPersistenceOutcome.TariffAssignmentNotFound =>
                Result<CalculateMonthlyChargeResult>.Failure(
                    ChargeErrors.TariffAssignmentNotFound),
            BillingCalculationPersistenceOutcome.TariffVersionNotFound =>
                Result<CalculateMonthlyChargeResult>.Failure(
                    ChargeErrors.TariffVersionNotFound),
            BillingCalculationPersistenceOutcome.TariffPeriodChangeRequiresPolicy =>
                Result<CalculateMonthlyChargeResult>.Failure(
                    ChargeErrors.TariffPeriodChangeRequiresPolicy),
            BillingCalculationPersistenceOutcome.InvalidConsumption =>
                Result<CalculateMonthlyChargeResult>.Failure(
                    ChargeErrors.InvalidAmount),
            _ => throw new InvalidOperationException(
                $"Unexpected billing persistence outcome: {persistence.Outcome}.")
        };
    }
}
