using System.Text.Json;
using EcoBilling.Api.Endpoints;
using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.IntegrationTests.Api;

public sealed class ResidentAddressContractTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("\"Bishkek, Chuy Avenue, 42\"", "Bishkek, Chuy Avenue, 42", "-", "-")]
    [InlineData("{\"locality\":\"Bishkek\",\"street\":\"Chuy Avenue\",\"house\":\"42\"}", "Bishkek", "Chuy Avenue", "42")]
    [InlineData("{\"Locality\":\"Bishkek\",\"Street\":\"Chuy Avenue\",\"House\":\"42\"}", "Bishkek", "Chuy Avenue", "42")]
    public void CreateResident_AcceptsBothAddressContracts(
        string addressJson, string locality, string street, string house)
    {
        var request = JsonSerializer.Deserialize<CreateResidentRequest>(
            "{\"fullName\":\"Resident\",\"accountNumber\":\"AB-000001\",\"password\":\"12345678\",\"address\":" + addressJson + "}", Options);

        Assert.NotNull(request);
        Assert.NotNull(request.Address);
        Assert.Equal(locality, request.Address.Locality);
        Assert.Equal(street, request.Address.Street);
        Assert.Equal(house, request.Address.House);
        Assert.True(Address.Create(new AddressId(Guid.NewGuid()),
            request.Address.Locality, request.Address.Street, request.Address.House,
            request.Address.Building, request.Address.Apartment).IsSuccess);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("42")]
    [InlineData("[]")]
    [InlineData("{\"locality\":42}")]
    public void InvalidAddressTypes_AreRejected(string json) =>
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CreateResidentAddressRequest>(json, Options));

    [Theory]
    [InlineData("\"   \"")]
    [InlineData("{\"locality\":\"Bishkek\"}")]
    public void EmptyOrIncompleteAddress_DoesNotBypassDomainValidation(string json)
    {
        var value = JsonSerializer.Deserialize<CreateResidentAddressRequest>(json, Options);
        Assert.NotNull(value);
        Assert.True(Address.Create(new AddressId(Guid.NewGuid()),
            value.Locality, value.Street, value.House, value.Building, value.Apartment).IsFailure);
    }

    [Fact]
    public void StructuredAddress_RoundTripPreservesOptionalFields()
    {
        var value = new CreateResidentAddressRequest("Bishkek", "Chuy", "42", "B", "7");
        var json = JsonSerializer.Serialize(value, Options);
        Assert.Equal(value, JsonSerializer.Deserialize<CreateResidentAddressRequest>(json, Options));
        Assert.DoesNotContain("Bishkek", value.ToString());
    }
}
