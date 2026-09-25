using EcoBilling.Modules.Meters.Domain;

namespace EcoBilling.Modules.Meters.Features.Abstractions;

public interface IMeterRepository
{
    Task<Meter?> GetByIdAsync(
        MeterId meterId,
        CancellationToken cancellationToken);
}
