using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Meters.Features.Abstractions;
using EcoBilling.Modules.Meters.Features.GetById;

namespace EcoBilling.UnitTests.Meters.Features.GetById;

public sealed class GetMeterByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenMeterExists_ReturnsDetails()
    {
        var meter = CreateMeter();
        var repository = new RecordingMeterRepository(meter);
        var handler = new GetMeterByIdHandler(repository);

        var result = await handler.Handle(
            new GetMeterByIdQuery(meter.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(meter.Id.Value, result.Value.Id);
        Assert.Equal(meter.AccountId.Value, result.Value.AccountId);
        Assert.Equal(meter.SerialNumber.Value, result.Value.SerialNumber);
        Assert.Equal(meter.InstalledAt, result.Value.InstalledAt);
        Assert.Equal(meter.CreatedAt, result.Value.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenMeterDoesNotExist_ReturnsNotFound()
    {
        var handler = new GetMeterByIdHandler(new RecordingMeterRepository(null));

        var result = await handler.Handle(
            new GetMeterByIdQuery(new MeterId(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(MeterErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_ForwardsMeterIdAndCancellationToken()
    {
        var meterId = new MeterId(Guid.NewGuid());
        using var cancellationTokenSource = new CancellationTokenSource();
        var repository = new RecordingMeterRepository(null);
        var handler = new GetMeterByIdHandler(repository);

        await handler.Handle(
            new GetMeterByIdQuery(meterId),
            cancellationTokenSource.Token);

        Assert.Equal(meterId, repository.ReceivedMeterId);
        Assert.Equal(cancellationTokenSource.Token, repository.ReceivedCancellationToken);
    }

    private static Meter CreateMeter() =>
        Meter.Create(
            new MeterId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            "SN-AB-123",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow).Value;

    private sealed class RecordingMeterRepository(Meter? meter)
        : IMeterRepository
    {
        public MeterId? ReceivedMeterId { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<Meter?> GetByIdAsync(
            MeterId meterId,
            CancellationToken cancellationToken)
        {
            ReceivedMeterId = meterId;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(meter);
        }
    }
}
