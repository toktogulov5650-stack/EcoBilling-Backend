using EcoBilling.Modules.Reports.Contracts;
using EcoBilling.Modules.Reports.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Reports.Features.GetFinancialSummary;

public sealed class GetDistrictFinancialSummaryHandler(
    IDistrictFinancialSummaryReader reader)
{
    public async Task<Result<DistrictFinancialSummary>> Handle(
        CancellationToken cancellationToken) =>
        Result<DistrictFinancialSummary>.Success(
            await reader.ReadAsync(cancellationToken));
}
