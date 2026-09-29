namespace EcoBilling.Modules.Identity.Contracts;

public sealed record DirectorProfileDetails(
    Guid DirectorId,
    Guid UserId,
    string FullName,
    string Email,
    DateTimeOffset CreatedAt);
