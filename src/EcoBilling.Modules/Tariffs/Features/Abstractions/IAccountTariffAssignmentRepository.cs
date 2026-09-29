using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.Modules.Tariffs.Features.Abstractions;

public interface IAccountTariffAssignmentRepository
{
    Task<AccountTariffAssignmentPersistenceOutcome> AssignAsync(
        AccountTariffAssignment assignment,
        string actorId,
        string correlationId,
        CancellationToken cancellationToken);

    Task<AccountTariffAssignmentCloseOutcome> CloseAsync(
        AccountId accountId,
        AccountTariffAssignmentId assignmentId,
        DateOnly effectiveTo,
        string actorId,
        string correlationId,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken);

    Task<AccountTariffAssignment?> GetEffectiveAsync(
        AccountId accountId,
        DateOnly date,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AccountTariffAssignment>> ListByAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken);
}

public enum AccountTariffAssignmentPersistenceOutcome
{
    Assigned = 0,
    AccountNotFound = 1,
    TariffNotFound = 2,
    Overlap = 3
}

public enum AccountTariffAssignmentCloseOutcome
{
    Closed = 0,
    NotFound = 1,
    InvalidEffectivePeriod = 2,
    AlreadyClosed = 3
}
