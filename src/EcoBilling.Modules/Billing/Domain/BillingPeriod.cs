using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Billing.Domain;

public sealed record BillingPeriod
{
    private BillingPeriod(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public DateOnly Start { get; }

    public DateOnly End { get; }

    public static Result<BillingPeriod> Create(DateOnly start, DateOnly end)
    {
        if (end <= start)
        {
            return Result<BillingPeriod>.Failure(ChargeErrors.InvalidBillingPeriod);
        }

        return Result<BillingPeriod>.Success(new BillingPeriod(start, end));
    }

    public override string ToString() => $"[{Start:yyyy-MM-dd}, {End:yyyy-MM-dd})";
}
