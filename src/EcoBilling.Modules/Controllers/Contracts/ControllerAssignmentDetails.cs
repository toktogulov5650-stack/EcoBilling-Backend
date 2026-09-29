namespace EcoBilling.Modules.Controllers.Contracts;

public sealed record ControllerAssignmentDetails(
    Guid AssignmentId,
    Guid ControllerId,
    Guid AddressId,
    string Locality,
    string Street,
    string House,
    string? Building,
    string? Apartment,
    DateTimeOffset CreatedAt);
