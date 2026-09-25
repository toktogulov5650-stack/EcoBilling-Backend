namespace EcoBilling.Modules.Controllers.Contracts;

public sealed record ControllerProfile(
    Guid Id,
    Guid UserId,
    string FullName,
    DateTimeOffset CreatedAt);
