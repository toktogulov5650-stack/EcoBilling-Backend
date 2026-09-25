using EcoBilling.Modules.Tariffs.Domain;

namespace EcoBilling.UnitTests.Tariffs.Domain;

public sealed class TariffVersionIdTests
{
    [Fact]
    public void Constructor_WithValue_PreservesValue()
    {
        var value = Guid.NewGuid();

        var id = new TariffVersionId(value);

        Assert.Equal(value, id.Value);
        Assert.Equal(value.ToString(), id.ToString());
    }

    [Fact]
    public void Constructor_WithEmptyValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TariffVersionId(Guid.Empty));
    }
}
