using EcoBilling.Modules.Meters.Domain;

namespace EcoBilling.UnitTests.Meters.Domain;

public sealed class MeterIdTests
{
    [Fact]
    public void Constructor_WithValue_PreservesValue()
    {
        var value = Guid.NewGuid();

        var id = new MeterId(value);

        Assert.Equal(value, id.Value);
        Assert.Equal(value.ToString(), id.ToString());
    }

    [Fact]
    public void Constructor_WithEmptyValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MeterId(Guid.Empty));
    }
}
