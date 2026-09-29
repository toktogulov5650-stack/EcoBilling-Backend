using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.Modules.Tariffs.Features.Abstractions;

public sealed record TariffPersistenceResult(
    TariffPersistenceOutcome Outcome,
    TariffId? TariffId = null);

public enum TariffPersistenceOutcome
{
    Created = 0,
    NameAlreadyExists = 1
}
