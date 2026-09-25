namespace EcoBilling.Modules.Residents.Contracts;

public sealed record ResidentProfile(
    Guid Id,
    Guid UserId,
    string FullName,
    DateTimeOffset CreatedAt);
