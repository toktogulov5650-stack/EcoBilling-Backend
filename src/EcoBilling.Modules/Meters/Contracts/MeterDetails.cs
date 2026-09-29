namespace EcoBilling.Modules.Meters.Contracts;

public sealed record MeterDetails(
    Guid Id,
    Guid AccountId,
    string SerialNumber,
    DateTimeOffset InstalledAt,
    DateTimeOffset CreatedAt,
    bool IsActive = true,
    DateTimeOffset? RetiredAt = null,
    Guid? ReplacesMeterId = null);
