using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Billing.Domain;
using EcoBilling.Modules.Billing.Features.Abstractions;
using EcoBilling.Modules.Billing.Features.GetById;
using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.UnitTests.Billing.Features.GetById;

public sealed class GetChargeByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenChargeExists_ReturnsDetails()
    {
        var charge = CreateCharge();
        var repository = new RecordingChargeRepository(charge);
        var handler = new GetChargeByIdHandler(repository);

        var result = await handler.Handle(
            new GetChargeByIdQuery(charge.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(charge.Id.Value, result.Value.Id);
        Assert.Equal(charge.AccountId.Value, result.Value.AccountId);
        Assert.Equal(charge.TariffVersionId.Value, result.Value.TariffVersionId);
        Assert.Equal(charge.PeriodStart, result.Value.PeriodStart);
        Assert.Equal(charge.PeriodEnd, result.Value.PeriodEnd);
        Assert.Equal(charge.Amount, result.Value.Amount);
        Assert.Equal(charge.CreatedAt, result.Value.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenChargeDoesNotExist_ReturnsNotFound()
    {
        var handler = new GetChargeByIdHandler(new RecordingChargeRepository(null));

        var result = await handler.Handle(
            new GetChargeByIdQuery(new ChargeId(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ChargeErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_ForwardsChargeIdAndCancellationToken()
    {
        var chargeId = new ChargeId(Guid.NewGuid());
        using var cancellationTokenSource = new CancellationTokenSource();
        var repository = new RecordingChargeRepository(null);
        var handler = new GetChargeByIdHandler(repository);

        await handler.Handle(
            new GetChargeByIdQuery(chargeId),
            cancellationTokenSource.Token);

        Assert.Equal(chargeId, repository.ReceivedChargeId);
        Assert.Equal(cancellationTokenSource.Token, repository.ReceivedCancellationToken);
    }

    private static Charge CreateCharge() =>
        Charge.Create(
            new ChargeId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            new TariffVersionId(Guid.NewGuid()),
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1),
            123.456m,
            DateTimeOffset.UtcNow).Value;

    private sealed class RecordingChargeRepository(Charge? charge)
        : IChargeRepository
    {
        public ChargeId? ReceivedChargeId { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<Charge?> GetByIdAsync(
            ChargeId chargeId,
            CancellationToken cancellationToken)
        {
            ReceivedChargeId = chargeId;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(charge);
        }
    }
}
