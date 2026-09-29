namespace EcoBilling.Modules.Reports.Contracts;

public sealed record DistrictFinancialSummary(
    decimal TotalCharges,
    decimal TotalPayments,
    decimal OutstandingDebt,
    decimal Overpayment,
    decimal TotalConsumption,
    string Currency,
    IReadOnlyList<ControllerPerformanceSummary> Controllers);

public sealed record ControllerPerformanceSummary(
    Guid ControllerId,
    string FullName,
    int AssignedAddresses,
    long ReadingsEntered);
