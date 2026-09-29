using EcoBilling.Modules.Tariffs.Contracts;
using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Tariffs.Features.GetForAccount;

public sealed class GetAccountTariffHandler(
    IAccountTariffAssignmentRepository repository)
{
    public async Task<Result<AccountTariffAssignmentDetails>> Handle(
        GetAccountTariffQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.AccountId);

        var assignment = await repository.GetEffectiveAsync(
            query.AccountId,
            query.Date,
            cancellationToken);

        if (assignment is null)
        {
            return Result<AccountTariffAssignmentDetails>.Failure(
                AccountTariffAssignmentErrors.NotFound);
        }

        return Result<AccountTariffAssignmentDetails>.Success(
            new AccountTariffAssignmentDetails(
                assignment.Id.Value,
                assignment.AccountId.Value,
                assignment.TariffId.Value,
                assignment.EffectiveFrom,
                assignment.EffectiveTo,
                assignment.CreatedAt));
    }
}
