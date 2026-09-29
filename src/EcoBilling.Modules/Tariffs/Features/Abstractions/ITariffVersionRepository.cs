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
}
