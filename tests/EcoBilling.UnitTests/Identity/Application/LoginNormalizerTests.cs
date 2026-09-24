using EcoBilling.Modules.Identity.Application;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Application;

public sealed class LoginNormalizerTests
{
    private readonly LoginNormalizer normalizer = new();

    [Fact]
    public void Normalize_UsesTheSameEmailRulesAsLoginIdentity()
    {
        var result = normalizer.Normalize(LoginType.Email, " user@example.com ");

        Assert.True(result.IsSuccess);
        Assert.Equal("USER@EXAMPLE.COM", result.Value);
    }

    [Fact]
    public void Normalize_DoesNotInventAccountNumberFormat()
    {
        var result = normalizer.Normalize(LoginType.AccountNumber, "  Ab/12-3  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Ab/12-3", result.Value);
    }
}
