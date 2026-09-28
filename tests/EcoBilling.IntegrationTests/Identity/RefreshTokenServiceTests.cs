using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.IntegrationTests.Identity;

public sealed class RefreshTokenServiceTests
{
    [Fact]
    public void Generate_ReturnsOpaqueUniqueTokenAndSha256Hash()
    {
        var service = new RefreshTokenService();

        var first = service.Generate();
        var second = service.Generate();

        Assert.NotEqual(first.Value, second.Value);
        Assert.NotEqual(first.Hash, second.Hash);
        Assert.Equal(RefreshSession.TokenHashLength, first.Hash.Length);
        Assert.Equal(first.Hash, service.Hash(first.Value));
        Assert.DoesNotContain(first.Value, first.Hash, StringComparison.Ordinal);
    }
}
