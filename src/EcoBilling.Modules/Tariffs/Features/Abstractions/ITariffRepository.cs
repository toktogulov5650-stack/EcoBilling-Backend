using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.Modules.Tariffs.Features.Abstractions;

public interface ITariffRepository
{
    Task<Tariff?> GetByIdAsync(
        TariffId tariffId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Tariff>> ListAsync(
        CancellationToken cancellationToken);

    Task<TariffPersistenceResult> CreateAsync(
        Tariff tariff,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken);
}
