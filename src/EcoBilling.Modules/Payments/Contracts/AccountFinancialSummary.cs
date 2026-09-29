namespace EcoBilling.Modules.Payments.Contracts;

public sealed record AccountFinancialSummary(
    Guid AccountId,
    decimal TotalCharges,
    decimal TotalAllocatedPayments,
    decimal OutstandingDebt,
    decimal Overpayment,
    string Currency);
