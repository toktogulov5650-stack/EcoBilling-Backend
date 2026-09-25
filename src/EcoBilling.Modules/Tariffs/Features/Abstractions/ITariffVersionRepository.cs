using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.Modules.Tariffs.Features.Abstractions;

public interface ITariffVersionRepository
{
    Task<TariffVersion?> GetByIdAsync(
        TariffVersionId tariffVersionId,
        CancellationToken cancellationToken);
}
