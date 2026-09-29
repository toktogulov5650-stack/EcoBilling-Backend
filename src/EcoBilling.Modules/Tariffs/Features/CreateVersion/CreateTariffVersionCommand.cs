using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.Modules.Tariffs.Features.CreateVersion;

public sealed record CreateTariffVersionCommand(
    TariffId TariffId,
    decimal Rate,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string ActorId,
    string CorrelationId);
