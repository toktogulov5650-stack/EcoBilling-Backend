using EcoBilling.Modules.Identity.Domain;
using EcoBilling.Modules.Residents.Domain;
using EcoBilling.Modules.Residents.Features.Abstractions;
using EcoBilling.Modules.Residents.Features.GetProfile;

namespace EcoBilling.UnitTests.Residents.Features.GetProfile;

public sealed class GetResidentProfileHandlerTests
{
    [Fact]
    public async Task Handle_WhenResidentExists_ReturnsProfile()
    {
        var account = CreateResidentAccount();
        var resident = Resident.Create(
            new ResidentId(Guid.NewGuid()),
            account,
            "Ada Lovelace",
            DateTimeOffset.UtcNow).Value;
        var repository = new RecordingResidentRepository(resident);
        var handler = new GetResidentProfileHandler(repository);

        var result = await handler.Handle(
            new GetResidentProfileQuery(account.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(resident.Id.Value, result.Value.Id);
        Assert.Equal(account.Id.Value, result.Value.UserId);
        Assert.Equal(resident.FullName, result.Value.FullName);
        Assert.Equal(resident.CreatedAt, result.Value.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenResidentDoesNotExist_ReturnsNotFound()
    {
        var handler = new GetResidentProfileHandler(new RecordingResidentRepository(null));

        var result = await handler.Handle(
            new GetResidentProfileQuery(new UserId(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResidentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_ForwardsUserIdAndCancellationToken()
    {
        var userId = new UserId(Guid.NewGuid());
        using var cancellationTokenSource = new CancellationTokenSource();
        var repository = new RecordingResidentRepository(null);
        var handler = new GetResidentProfileHandler(repository);

        await handler.Handle(
            new GetResidentProfileQuery(userId),
            cancellationTokenSource.Token);

        Assert.Equal(userId, repository.ReceivedUserId);
        Assert.Equal(cancellationTokenSource.Token, repository.ReceivedCancellationToken);
    }

    private static UserAccount CreateResidentAccount()
    {
        var loginIdentity = LoginIdentity.Create(LoginType.AccountNumber, "A-100").Value;
        return UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity,
            "stored-password-hash",
            UserRole.Resident,
            DateTimeOffset.UtcNow).Value;
    }

    private sealed class RecordingResidentRepository(Resident? resident)
        : IResidentRepository
    {
        public UserId? ReceivedUserId { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<Resident?> GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken)
        {
            ReceivedUserId = userId;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(resident);
        }
    }
}
