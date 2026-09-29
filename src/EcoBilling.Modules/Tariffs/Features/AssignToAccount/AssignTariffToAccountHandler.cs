using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Features.AssignToAccount;

public sealed class AssignTariffToAccountHandler
{
    private readonly IAccountTariffAssignmentRepository repository;
    private readonly TimeProvider timeProvider;

    public AssignTariffToAccountHandler(
        IAccountTariffAssignmentRepository repository,
        TimeProvider timeProvider)
    {
        this.repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<Result<AssignTariffToAccountResult>> Handle(
        AssignTariffToAccountCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.AccountId);
        ArgumentNullException.ThrowIfNull(command.TariffId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var assignment = AccountTariffAssignment.Create(
            new AccountTariffAssignmentId(Guid.NewGuid()),
            command.AccountId,
            command.TariffId,
            command.EffectiveFrom,
            command.EffectiveTo,
            timeProvider.GetUtcNow());
        if (assignment.IsFailure)
        {
            return Result<AssignTariffToAccountResult>.Failure(assignment.Error);
        }

        var outcome = await repository.AssignAsync(
            assignment.Value,
            command.ActorId,
            command.CorrelationId,
            cancellationToken);

        return outcome switch
        {
            AccountTariffAssignmentPersistenceOutcome.Assigned =>
                Result<AssignTariffToAccountResult>.Success(
                    new AssignTariffToAccountResult(
                        assignment.Value.Id,
                        assignment.Value.EffectiveFrom,
                        assignment.Value.EffectiveTo)),
            AccountTariffAssignmentPersistenceOutcome.AccountNotFound =>
                Result<AssignTariffToAccountResult>.Failure(
                    AccountTariffAssignmentErrors.AccountNotFound),
            AccountTariffAssignmentPersistenceOutcome.TariffNotFound =>
                Result<AssignTariffToAccountResult>.Failure(
                    AccountTariffAssignmentErrors.TariffNotFound),
            AccountTariffAssignmentPersistenceOutcome.Overlap =>
                Result<AssignTariffToAccountResult>.Failure(
                    AccountTariffAssignmentErrors.Overlap),
            _ => throw new InvalidOperationException(
                $"Unknown tariff assignment persistence outcome: {outcome}.")
        };
    }
}
