namespace EcoBilling.Modules.Residents.Contracts;

public sealed record ResidentFinancialView(
    decimal TotalCharges,
    decimal TotalPayments,
    decimal OutstandingDebt,
    decimal Overpayment,
    string Currency);
