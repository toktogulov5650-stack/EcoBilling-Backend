using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Domain;

public sealed class UserIdTests
{
    [Fact]
    public void Constructor_AcceptsNonEmptyGuid()
    {
        var value = Guid.NewGuid();

        var userId = new UserId(value);

        Assert.Equal(value, userId.Value);
    }

    [Fact]
    public void Constructor_RejectsEmptyGuid()
    {
        Assert.Throws<ArgumentException>(() => new UserId(Guid.Empty));
    }
}
