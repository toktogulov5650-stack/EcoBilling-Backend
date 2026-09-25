using EcoBilling.Modules.Payments.Domain;

namespace EcoBilling.UnitTests.Payments.Domain;

public sealed class PaymentIdTests
{
    [Fact]
    public void Constructor_WithValue_PreservesValue()
    {
        var value = Guid.NewGuid();

        var id = new PaymentId(value);

        Assert.Equal(value, id.Value);
        Assert.Equal(value.ToString(), id.ToString());
    }

    [Fact]
    public void Constructor_WithEmptyValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PaymentId(Guid.Empty));
    }
}
