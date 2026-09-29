namespace EcoBilling.Modules.Residents.Contracts;

public sealed record ResidentMeterView(
    Guid MeterId,
    string SerialNumber,
    DateTimeOffset InstalledAt,
    bool IsActive,
    DateTimeOffset? RetiredAt);
