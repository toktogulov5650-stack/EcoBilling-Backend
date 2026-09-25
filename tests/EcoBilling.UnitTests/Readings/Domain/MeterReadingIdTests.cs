using EcoBilling.Modules.Readings.Domain;

namespace EcoBilling.UnitTests.Readings.Domain;

public sealed class MeterReadingIdTests
{
    [Fact]
    public void Constructor_WithValue_PreservesValue()
    {
        var value = Guid.NewGuid();

        var id = new MeterReadingId(value);

        Assert.Equal(value, id.Value);
        Assert.Equal(value.ToString(), id.ToString());
    }

    [Fact]
    public void Constructor_WithEmptyValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MeterReadingId(Guid.Empty));
    }
}
