using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.Modules.Tariffs.Features.Abstractions;

public sealed record TariffVersionPersistenceResult(
    TariffVersionPersistenceOutcome Outcome,
    TariffVersionId? TariffVersionId = null);

public enum TariffVersionPersistenceOutcome
{
    Created = 0,
    TariffNotFound = 1,
    OverlappingPeriod = 2
}
