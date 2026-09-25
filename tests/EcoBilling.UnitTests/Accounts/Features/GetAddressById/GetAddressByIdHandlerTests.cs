using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Accounts.Features.Abstractions;
using EcoBilling.Modules.Accounts.Features.GetAddressById;

namespace EcoBilling.UnitTests.Accounts.Features.GetAddressById;

public sealed class GetAddressByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenAddressExists_ReturnsDetails()
    {
        var address = CreateAddress();
        var repository = new RecordingAddressRepository(address);
        var handler = new GetAddressByIdHandler(repository);

        var result = await handler.Handle(
            new GetAddressByIdQuery(address.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(address.Id.Value, result.Value.Id);
        Assert.Equal(address.Locality, result.Value.Locality);
        Assert.Equal(address.Street, result.Value.Street);
        Assert.Equal(address.House, result.Value.House);
        Assert.Equal(address.Building, result.Value.Building);
        Assert.Equal(address.Apartment, result.Value.Apartment);
        Assert.Equal(address.SearchText, result.Value.SearchText);
    }

    [Fact]
    public async Task Handle_WhenAddressDoesNotExist_ReturnsNotFound()
    {
        var handler = new GetAddressByIdHandler(new RecordingAddressRepository(null));

        var result = await handler.Handle(
            new GetAddressByIdQuery(new AddressId(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AddressErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Handle_ForwardsAddressIdAndCancellationToken()
    {
        var addressId = new AddressId(Guid.NewGuid());
        using var cancellationTokenSource = new CancellationTokenSource();
        var repository = new RecordingAddressRepository(null);
        var handler = new GetAddressByIdHandler(repository);

        await handler.Handle(
            new GetAddressByIdQuery(addressId),
            cancellationTokenSource.Token);

        Assert.Equal(addressId, repository.ReceivedAddressId);
        Assert.Equal(cancellationTokenSource.Token, repository.ReceivedCancellationToken);
    }

    private static Address CreateAddress() =>
        Address.Create(
            new AddressId(Guid.NewGuid()),
            "Бишкек",
            "Исанова",
            "10",
            "А",
            "42").Value;

    private sealed class RecordingAddressRepository(Address? address)
        : IAddressRepository
    {
        public AddressId? ReceivedAddressId { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<Address?> GetByIdAsync(
            AddressId addressId,
            CancellationToken cancellationToken)
        {
            ReceivedAddressId = addressId;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(address);
        }
    }
}
