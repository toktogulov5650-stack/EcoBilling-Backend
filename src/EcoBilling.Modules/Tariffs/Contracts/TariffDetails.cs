namespace EcoBilling.Modules.Tariffs.Contracts;

public sealed record TariffDetails(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt);
