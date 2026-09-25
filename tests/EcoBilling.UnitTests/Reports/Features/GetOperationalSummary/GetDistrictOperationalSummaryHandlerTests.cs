using EcoBilling.Modules.Reports.Contracts;
using EcoBilling.Modules.Reports.Features.Abstractions;
using EcoBilling.Modules.Reports.Features.GetOperationalSummary;

namespace EcoBilling.UnitTests.Reports.Features.GetOperationalSummary;

public sealed class GetDistrictOperationalSummaryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSummary()
    {
        var summary = new DistrictOperationalSummary(
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9,
            10);
        var handler = new GetDistrictOperationalSummaryHandler(
            new RecordingSummaryReader(summary));

        var result = await handler.Handle(
            new GetDistrictOperationalSummaryQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(summary, result.Value);
    }

    [Fact]
    public async Task Handle_ForwardsCancellationToken()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var reader = new RecordingSummaryReader(
            new DistrictOperationalSummary(0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        var handler = new GetDistrictOperationalSummaryHandler(reader);

        await handler.Handle(
            new GetDistrictOperationalSummaryQuery(),
            cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, reader.ReceivedCancellationToken);
    }

    private sealed class RecordingSummaryReader(DistrictOperationalSummary summary)
        : IDistrictOperationalSummaryReader
    {
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<DistrictOperationalSummary> ReadAsync(
            CancellationToken cancellationToken)
        {
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(summary);
        }
    }
}
