using EcoBilling.Modules.Payments.Contracts;
using EcoBilling.Modules.Payments.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Payments.Features.GetFinancialSummary;

public sealed class GetAccountFinancialSummaryHandler(IPaymentRepository repository)
{
    public async Task<Result<AccountFinancialSummary>> Handle(
        GetAccountFinancialSummaryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.AccountId);

        var summary = await repository.GetFinancialSummaryAsync(
            query.AccountId,
            cancellationToken);

        return summary is null
            ? Result<AccountFinancialSummary>.Failure(
                EcoBilling.Modules.Accounts.Domain.AccountErrors.NotFound)
            : Result<AccountFinancialSummary>.Success(summary);
    }
}
