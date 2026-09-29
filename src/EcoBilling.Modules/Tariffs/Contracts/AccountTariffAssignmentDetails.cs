namespace EcoBilling.Modules.Tariffs.Contracts;

public sealed record AccountTariffAssignmentDetails(
    Guid Id,
    Guid AccountId,
    Guid TariffId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    DateTimeOffset CreatedAt);
