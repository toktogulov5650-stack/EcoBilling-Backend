using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.UnitTests.Residents.Domain;

public sealed class ResidentIdTests
{
    [Fact]
    public void Constructor_WithValue_PreservesValue()
    {
        var value = Guid.NewGuid();

        var id = new ResidentId(value);

        Assert.Equal(value, id.Value);
    }

    [Fact]
    public void Constructor_WithEmptyValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ResidentId(Guid.Empty));
    }
}
