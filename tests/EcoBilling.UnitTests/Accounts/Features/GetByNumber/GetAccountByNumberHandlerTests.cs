using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Accounts.Features.Abstractions;
using EcoBilling.Modules.Accounts.Features.GetByNumber;
using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.UnitTests.Accounts.Features.GetByNumber;

public sealed class GetAccountByNumberHandlerTests
{
    [Fact]
    public async Task Handle_WhenAccountExists_ReturnsDetails()
    {
        var account = CreateAccount("AB-123");
        var repository = new RecordingAccountRepository(account);
        var handler = new GetAccountByNumberHandler(repository);

        var result = await handler.Handle(
            new GetAccountByNumberQuery("  ab-123  "),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(account.Id.Value, result.Value.Id);
        Assert.Equal(account.ResidentId.Value, result.Value.ResidentId);
        Assert.Equal(account.AddressId.Value, result.Value.AddressId);
        Assert.Equal("AB-123", result.Value.AccountNumber);
        Assert.Equal(account.CreatedAt, result.Value.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenAccountDoesNotExist_ReturnsNotFound()
    {
        var handler = new GetAccountByNumberHandler(new RecordingAccountRepository(null));

        var result = await handler.Handle(
            new GetAccountByNumberQuery("AB-404"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AccountErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_WithInvalidNumber_ReturnsValidationErrorWithoutRepositoryCall()
    {
        var repository = new RecordingAccountRepository(null);
        var handler = new GetAccountByNumberHandler(repository);

        var result = await handler.Handle(
            new GetAccountByNumberQuery("   "),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AccountErrors.InvalidAccountNumber, result.Error);
        Assert.Null(repository.ReceivedAccountNumber);
    }

    [Fact]
    public async Task Handle_ForwardsNormalizedNumberAndCancellationToken()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var repository = new RecordingAccountRepository(null);
        var handler = new GetAccountByNumberHandler(repository);

        await handler.Handle(
            new GetAccountByNumberQuery("  ab-123  "),
            cancellationTokenSource.Token);

        Assert.Equal("AB-123", repository.ReceivedAccountNumber?.Value);
        Assert.Equal(cancellationTokenSource.Token, repository.ReceivedCancellationToken);
    }

    private static Account CreateAccount(string accountNumber) =>
        Account.Create(
            new AccountId(Guid.NewGuid()),
            new ResidentId(Guid.NewGuid()),
            new AddressId(Guid.NewGuid()),
            accountNumber,
            DateTimeOffset.UtcNow).Value;

    private sealed class RecordingAccountRepository(Account? account)
        : IAccountRepository
    {
        public AccountNumber? ReceivedAccountNumber { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<Account?> GetByNumberAsync(
            AccountNumber accountNumber,
            CancellationToken cancellationToken)
        {
            ReceivedAccountNumber = accountNumber;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(account);
        }
    }
}
