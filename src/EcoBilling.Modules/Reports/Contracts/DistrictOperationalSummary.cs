namespace EcoBilling.Modules.Reports.Contracts;

public sealed record DistrictOperationalSummary(
    long Residents,
    long Controllers,
    long Accounts,
    long Addresses,
    long Meters,
    long MeterReadings,
    long Tariffs,
    long TariffVersions,
    long Charges,
    long Payments);
