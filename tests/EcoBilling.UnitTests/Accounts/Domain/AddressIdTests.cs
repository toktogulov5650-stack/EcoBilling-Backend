using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.UnitTests.Accounts.Domain;

public sealed class AddressIdTests
{
    [Fact]
    public void Constructor_WithValue_PreservesValue()
    {
        var value = Guid.NewGuid();

        var id = new AddressId(value);

        Assert.Equal(value, id.Value);
        Assert.Equal(value.ToString(), id.ToString());
    }

    [Fact]
    public void Constructor_WithEmptyValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AddressId(Guid.Empty));
    }
}
