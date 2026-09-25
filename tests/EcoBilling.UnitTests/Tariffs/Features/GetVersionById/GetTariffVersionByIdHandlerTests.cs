using EcoBilling.Modules.Tariffs.Domain;
using EcoBilling.Modules.Tariffs.Features.Abstractions;
using EcoBilling.Modules.Tariffs.Features.GetVersionById;

namespace EcoBilling.UnitTests.Tariffs.Features.GetVersionById;

public sealed class GetTariffVersionByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenVersionExists_ReturnsDetails()
    {
        var version = CreateVersion();
        var repository = new RecordingTariffVersionRepository(version);
        var handler = new GetTariffVersionByIdHandler(repository);

        var result = await handler.Handle(
            new GetTariffVersionByIdQuery(version.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(version.Id.Value, result.Value.Id);
        Assert.Equal(version.TariffId.Value, result.Value.TariffId);
        Assert.Equal(version.Rate.Value, result.Value.Rate);
        Assert.Equal(version.EffectiveFrom, result.Value.EffectiveFrom);
        Assert.Equal(version.EffectiveTo, result.Value.EffectiveTo);
        Assert.Equal(version.CreatedAt, result.Value.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenVersionDoesNotExist_ReturnsNotFound()
    {
        var handler = new GetTariffVersionByIdHandler(
            new RecordingTariffVersionRepository(null));

        var result = await handler.Handle(
            new GetTariffVersionByIdQuery(new TariffVersionId(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TariffErrors.VersionNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_ForwardsVersionIdAndCancellationToken()
    {
        var versionId = new TariffVersionId(Guid.NewGuid());
        using var cancellationTokenSource = new CancellationTokenSource();
        var repository = new RecordingTariffVersionRepository(null);
        var handler = new GetTariffVersionByIdHandler(repository);

        await handler.Handle(
            new GetTariffVersionByIdQuery(versionId),
            cancellationTokenSource.Token);

        Assert.Equal(versionId, repository.ReceivedVersionId);
        Assert.Equal(cancellationTokenSource.Token, repository.ReceivedCancellationToken);
    }

    private static TariffVersion CreateVersion() =>
        TariffVersion.Create(
            new TariffVersionId(Guid.NewGuid()),
            new TariffId(Guid.NewGuid()),
            12.345m,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 7, 1),
            DateTimeOffset.UtcNow).Value;

    private sealed class RecordingTariffVersionRepository(TariffVersion? version)
        : ITariffVersionRepository
    {
        public TariffVersionId? ReceivedVersionId { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<TariffVersion?> GetByIdAsync(
            TariffVersionId tariffVersionId,
            CancellationToken cancellationToken)
        {
            ReceivedVersionId = tariffVersionId;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(version);
        }
    }
}
