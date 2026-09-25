namespace EcoBilling.Modules.Tariffs.Contracts;

public sealed record TariffVersionDetails(
    Guid Id,
    Guid TariffId,
    decimal Rate,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    DateTimeOffset CreatedAt);
