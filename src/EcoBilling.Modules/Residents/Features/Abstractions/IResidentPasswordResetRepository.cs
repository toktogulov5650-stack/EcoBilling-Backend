using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.Modules.Residents.Features.Abstractions;

public interface IResidentPasswordResetRepository
{
    Task<ResidentPasswordResetPersistenceResult> ResetAsync(
        ResidentPasswordResetOperation operation,
        string newPasswordHash,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken);
}
