using EcoBilling.Modules.Tariffs.Contracts;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Features.ListAssignments;

public sealed class ListAccountTariffAssignmentsHandler(
    IAccountTariffAssignmentRepository repository)
{
    public async Task<Result<IReadOnlyList<AccountTariffAssignmentDetails>>> Handle(
        ListAccountTariffAssignmentsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.AccountId);

        var assignments = await repository.ListByAccountAsync(
            query.AccountId,
            cancellationToken);

        return Result<IReadOnlyList<AccountTariffAssignmentDetails>>.Success(
            assignments.Select(
                    assignment => new AccountTariffAssignmentDetails(
                        assignment.Id.Value,
                        assignment.AccountId.Value,
                        assignment.TariffId.Value,
                        assignment.EffectiveFrom,
                        assignment.EffectiveTo,
                        assignment.CreatedAt))
                .ToArray());
    }
}
