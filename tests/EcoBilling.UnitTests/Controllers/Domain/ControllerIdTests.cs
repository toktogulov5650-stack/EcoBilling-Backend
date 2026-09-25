using EcoBilling.Modules.Controllers.Domain;

namespace EcoBilling.UnitTests.Controllers.Domain;

public sealed class ControllerIdTests
{
    [Fact]
    public void Constructor_WithValue_PreservesValue()
    {
        var value = Guid.NewGuid();

        var id = new ControllerId(value);

        Assert.Equal(value, id.Value);
    }

    [Fact]
    public void Constructor_WithEmptyValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ControllerId(Guid.Empty));
    }
}
