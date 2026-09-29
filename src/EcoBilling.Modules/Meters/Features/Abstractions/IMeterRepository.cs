using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Meters.Domain;

namespace EcoBilling.Modules.Meters.Features.Abstractions;

public interface IMeterRepository
{
    Task<Meter?> GetByIdAsync(
        MeterId meterId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Meter>> ListByAccountIdAsync(
        AccountId accountId,
        CancellationToken cancellationToken);

    Task<MeterPersistenceResult> CreateAsync(
        Meter meter,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken);

    Task<MeterPersistenceResult> ReplaceAsync(
        MeterId currentMeterId,
        Meter replacementMeter,
        string actorId,
        string correlationId,
        DateTimeOffset retiredAt,
        CancellationToken cancellationToken);
}
