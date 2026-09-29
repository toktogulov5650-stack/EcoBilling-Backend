namespace EcoBilling.Modules.Controllers.Contracts;

public sealed record ControllerDirectoryEntry(
    Guid ControllerId,
    Guid UserId,
    string FullName,
    string Email,
    int AssignmentCount,
    DateTimeOffset CreatedAt);
