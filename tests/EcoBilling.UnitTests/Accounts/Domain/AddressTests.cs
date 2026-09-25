using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.UnitTests.Accounts.Domain;

public sealed class AddressTests
{
    [Fact]
    public void Create_WithValidData_NormalizesComponentsAndBuildsSearchText()
    {
        var id = new AddressId(Guid.NewGuid());

        var result = Address.Create(
            id,
            "  Бишкек  ",
            "  Токтогула   проспект  ",
            "  125  ",
            "  корпус   А  ",
            "  42  ");

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal("Бишкек", result.Value.Locality);
        Assert.Equal("Токтогула проспект", result.Value.Street);
        Assert.Equal("125", result.Value.House);
        Assert.Equal("корпус А", result.Value.Building);
        Assert.Equal("42", result.Value.Apartment);
        Assert.Equal(
            "БИШКЕК ТОКТОГУЛА ПРОСПЕКТ 125 КОРПУС А 42",
            result.Value.SearchText);
    }

    [Fact]
    public void Create_WithBlankOptionalComponents_StoresNullsAndOmitsThemFromSearchText()
    {
        var result = Address.Create(
            new AddressId(Guid.NewGuid()),
            "Бишкек",
            "Исанова",
            "10",
            " ",
            null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Building);
        Assert.Null(result.Value.Apartment);
        Assert.Equal("БИШКЕК ИСАНОВА 10", result.Value.SearchText);
    }

    [Fact]
    public void Create_WithMissingLocality_ReturnsValidationError()
    {
        var result = Address.Create(
            new AddressId(Guid.NewGuid()),
            " ",
            "Исанова",
            "10",
            null,
            null);

        Assert.True(result.IsFailure);
        Assert.Equal(AddressErrors.InvalidLocality, result.Error);
    }

    [Fact]
    public void Create_WithMissingStreet_ReturnsValidationError()
    {
        var result = Address.Create(
            new AddressId(Guid.NewGuid()),
            "Бишкек",
            null,
            "10",
            null,
            null);

        Assert.True(result.IsFailure);
        Assert.Equal(AddressErrors.InvalidStreet, result.Error);
    }

    [Fact]
    public void Create_WithMissingHouse_ReturnsValidationError()
    {
        var result = Address.Create(
            new AddressId(Guid.NewGuid()),
            "Бишкек",
            "Исанова",
            string.Empty,
            null,
            null);

        Assert.True(result.IsFailure);
        Assert.Equal(AddressErrors.InvalidHouse, result.Error);
    }
}
