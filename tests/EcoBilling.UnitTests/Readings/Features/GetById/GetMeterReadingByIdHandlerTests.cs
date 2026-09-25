using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Readings.Domain;
using EcoBilling.Modules.Readings.Features.Abstractions;
using EcoBilling.Modules.Readings.Features.GetById;

namespace EcoBilling.UnitTests.Readings.Features.GetById;

public sealed class GetMeterReadingByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenReadingExists_ReturnsDetails()
    {
        var reading = CreateReading();
        var repository = new RecordingMeterReadingRepository(reading);
        var handler = new GetMeterReadingByIdHandler(repository);

        var result = await handler.Handle(
            new GetMeterReadingByIdQuery(reading.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(reading.Id.Value, result.Value.Id);
        Assert.Equal(reading.MeterId.Value, result.Value.MeterId);
        Assert.Equal(reading.Value.Value, result.Value.Value);
        Assert.Equal(reading.MeasuredAt, result.Value.MeasuredAt);
        Assert.Equal(reading.CreatedAt, result.Value.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenReadingDoesNotExist_ReturnsNotFound()
    {
        var handler = new GetMeterReadingByIdHandler(
            new RecordingMeterReadingRepository(null));

        var result = await handler.Handle(
            new GetMeterReadingByIdQuery(new MeterReadingId(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(MeterReadingErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_ForwardsReadingIdAndCancellationToken()
    {
        var readingId = new MeterReadingId(Guid.NewGuid());
        using var cancellationTokenSource = new CancellationTokenSource();
        var repository = new RecordingMeterReadingRepository(null);
        var handler = new GetMeterReadingByIdHandler(repository);

        await handler.Handle(
            new GetMeterReadingByIdQuery(readingId),
            cancellationTokenSource.Token);

        Assert.Equal(readingId, repository.ReceivedReadingId);
        Assert.Equal(cancellationTokenSource.Token, repository.ReceivedCancellationToken);
    }

    private static MeterReading CreateReading() =>
        MeterReading.Create(
            new MeterReadingId(Guid.NewGuid()),
            new MeterId(Guid.NewGuid()),
            123.456m,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow).Value;

    private sealed class RecordingMeterReadingRepository(MeterReading? reading)
        : IMeterReadingRepository
    {
        public MeterReadingId? ReceivedReadingId { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<MeterReading?> GetByIdAsync(
            MeterReadingId readingId,
            CancellationToken cancellationToken)
        {
            ReceivedReadingId = readingId;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(reading);
        }
    }
}
