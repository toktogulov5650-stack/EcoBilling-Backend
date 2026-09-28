using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.Modules.Residents.Features.Abstractions;

public sealed record ResidentCreationPersistenceResult(
    ResidentCreationPersistenceOutcome Outcome,
    ResidentId? ResidentId = null,
    AccountId? AccountId = null,
    AddressId? AddressId = null,
    ResidentCreationOperationId? OperationId = null);

public enum ResidentCreationPersistenceOutcome
{
    Created = 1,
    Replayed = 2,
    IdempotencyConflict = 3,
    AccountNumberAlreadyExists = 4
}
