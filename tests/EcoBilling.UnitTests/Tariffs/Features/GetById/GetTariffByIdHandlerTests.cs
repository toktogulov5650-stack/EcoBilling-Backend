using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.Modules.Tariffs.Features.GetById;

namespace EcoBilling.UnitTests.Tariffs.Features.GetById;

public sealed class GetTariffByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenTariffExists_ReturnsDetails()
    {
        var tariff = CreateTariff();
        var repository = new RecordingTariffRepository(tariff);
        var handler = new GetTariffByIdHandler(repository);

        var result = await handler.Handle(
            new GetTariffByIdQuery(tariff.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(tariff.Id.Value, result.Value.Id);
        Assert.Equal(tariff.Name.Value, result.Value.Name);
        Assert.Equal(tariff.CreatedAt, result.Value.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenTariffDoesNotExist_ReturnsNotFound()
    {
        var handler = new GetTariffByIdHandler(new RecordingTariffRepository(null));

        var result = await handler.Handle(
            new GetTariffByIdQuery(new TariffId(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TariffErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_ForwardsTariffIdAndCancellationToken()
    {
        var tariffId = new TariffId(Guid.NewGuid());
        using var cancellationTokenSource = new CancellationTokenSource();
        var repository = new RecordingTariffRepository(null);
        var handler = new GetTariffByIdHandler(repository);

        await handler.Handle(
            new GetTariffByIdQuery(tariffId),
            cancellationTokenSource.Token);

        Assert.Equal(tariffId, repository.ReceivedTariffId);
        Assert.Equal(cancellationTokenSource.Token, repository.ReceivedCancellationToken);
    }

    private static Tariff CreateTariff() =>
        Tariff.Create(
            new TariffId(Guid.NewGuid()),
            "Население",
            DateTimeOffset.UtcNow).Value;

    private sealed class RecordingTariffRepository(Tariff? tariff)
        : ITariffRepository
    {
        public TariffId? ReceivedTariffId { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<Tariff?> GetByIdAsync(
            TariffId tariffId,
            CancellationToken cancellationToken)
        {
            ReceivedTariffId = tariffId;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(tariff);
        }
    }
}
