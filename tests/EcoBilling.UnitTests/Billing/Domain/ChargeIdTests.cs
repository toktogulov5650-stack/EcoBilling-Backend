using EcoBilling.Modules.Billing.Domain;

namespace EcoBilling.UnitTests.Billing.Domain;

public sealed class ChargeIdTests
{
    [Fact]
    public void Constructor_WithValue_PreservesValue()
    {
        var value = Guid.NewGuid();

        var id = new ChargeId(value);

        Assert.Equal(value, id.Value);
        Assert.Equal(value.ToString(), id.ToString());
    }

    [Fact]
    public void Constructor_WithEmptyValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ChargeId(Guid.Empty));
    }
}
