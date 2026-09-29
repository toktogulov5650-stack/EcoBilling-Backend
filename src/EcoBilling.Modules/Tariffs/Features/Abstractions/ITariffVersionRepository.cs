using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.Modules.Tariffs.Features.Abstractions;

public interface ITariffVersionRepository
{
    Task<TariffVersion?> GetByIdAsync(
        TariffVersionId tariffVersionId,
        CancellationToken cancellationToken);

    Task<TariffVersion?> GetEffectiveAsync(
        TariffId tariffId,
        DateOnly date,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TariffVersion>> ListByTariffIdAsync(
        TariffId tariffId,
        CancellationToken cancellationToken);

    Task<TariffVersionPersistenceResult> CreateAsync(
        TariffVersion version,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken);

    Task<TariffVersionCloseOutcome> CloseAsync(
        TariffId tariffId,
        TariffVersionId tariffVersionId,
        DateOnly effectiveTo,
        string actorId,
        string correlationId,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken);
}


public enum TariffVersionCloseOutcome
{
    Closed = 0,
    NotFound = 1,
    InvalidEffectivePeriod = 2,
    AlreadyClosed = 3
}
