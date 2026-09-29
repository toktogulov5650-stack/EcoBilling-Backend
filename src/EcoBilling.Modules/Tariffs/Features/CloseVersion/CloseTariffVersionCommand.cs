using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.Modules.Tariffs.Features.CloseVersion;

public sealed record CloseTariffVersionCommand(
    TariffId TariffId,
    TariffVersionId TariffVersionId,
    DateOnly EffectiveTo,
    string ActorId,
    string CorrelationId);
