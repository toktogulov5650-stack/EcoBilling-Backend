using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.Modules.Tariffs.Features.Abstractions;

public interface ITariffRepository
{
    Task<Tariff?> GetByIdAsync(
        TariffId tariffId,
        CancellationToken cancellationToken);
}
