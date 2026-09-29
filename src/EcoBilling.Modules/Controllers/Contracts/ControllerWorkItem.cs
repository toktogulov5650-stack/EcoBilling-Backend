namespace EcoBilling.Modules.Controllers.Contracts;

public sealed record ControllerWorkItem(
    Guid AssignmentId,
    Guid AddressId,
    Guid AccountId,
    string AccountNumber,
    Guid ResidentId,
    string ResidentFullName,
    string Locality,
    string Street,
    string House,
    string? Building,
    string? Apartment,
    IReadOnlyList<ControllerWorkMeter> Meters);

public sealed record ControllerWorkMeter(
    Guid MeterId,
    string SerialNumber,
    bool IsActive,
    DateTimeOffset InstalledAt);
