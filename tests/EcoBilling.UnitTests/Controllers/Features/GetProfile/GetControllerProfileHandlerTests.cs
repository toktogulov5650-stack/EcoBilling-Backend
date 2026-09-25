using EcoBilling.Modules.Controllers.Domain;
using EcoBilling.Modules.Controllers.Features.Abstractions;
using EcoBilling.Modules.Controllers.Features.GetProfile;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Controllers.Features.GetProfile;

public sealed class GetControllerProfileHandlerTests
{
    [Fact]
    public async Task Handle_WhenControllerExists_ReturnsProfile()
    {
        var account = CreateControllerAccount();
        var controller = Controller.Create(
            new ControllerId(Guid.NewGuid()),
            account,
            "Grace Hopper",
            DateTimeOffset.UtcNow).Value;
        var repository = new RecordingControllerRepository(controller);
        var handler = new GetControllerProfileHandler(repository);

        var result = await handler.Handle(
            new GetControllerProfileQuery(account.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(controller.Id.Value, result.Value.Id);
        Assert.Equal(account.Id.Value, result.Value.UserId);
        Assert.Equal(controller.FullName, result.Value.FullName);
        Assert.Equal(controller.CreatedAt, result.Value.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenControllerDoesNotExist_ReturnsNotFound()
    {
        var handler = new GetControllerProfileHandler(new RecordingControllerRepository(null));

        var result = await handler.Handle(
            new GetControllerProfileQuery(new UserId(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ControllerErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_ForwardsUserIdAndCancellationToken()
    {
        var userId = new UserId(Guid.NewGuid());
        using var cancellationTokenSource = new CancellationTokenSource();
        var repository = new RecordingControllerRepository(null);
        var handler = new GetControllerProfileHandler(repository);

        await handler.Handle(
            new GetControllerProfileQuery(userId),
            cancellationTokenSource.Token);

        Assert.Equal(userId, repository.ReceivedUserId);
        Assert.Equal(cancellationTokenSource.Token, repository.ReceivedCancellationToken);
    }

    private static UserAccount CreateControllerAccount()
    {
        var loginIdentity = LoginIdentity.Create(LoginType.Email, "agent@example.com").Value;
        return UserAccount.Create(
            new UserId(Guid.NewGuid()),
            loginIdentity,
            "stored-password-hash",
            UserRole.Controller,
            DateTimeOffset.UtcNow).Value;
    }

    private sealed class RecordingControllerRepository(Controller? controller)
        : IControllerRepository
    {
        public UserId? ReceivedUserId { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<Controller?> GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken)
        {
            ReceivedUserId = userId;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(controller);
        }
    }
}
