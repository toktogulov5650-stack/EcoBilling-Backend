using EcoBilling.Modules.Reports.Contracts;
using EcoBilling.Modules.Reports.Features.Abstractions;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.Modules.Reports.Features.GetOperationalSummary;

public sealed class GetDistrictOperationalSummaryHandler(
    IDistrictOperationalSummaryReader summaryReader)
{
    public async Task<Result<DistrictOperationalSummary>> Handle(
        GetDistrictOperationalSummaryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var summary = await summaryReader.ReadAsync(cancellationToken);
        return Result<DistrictOperationalSummary>.Success(summary);
    }
}
