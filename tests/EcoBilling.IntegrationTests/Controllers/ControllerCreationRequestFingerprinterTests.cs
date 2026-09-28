using EcoBilling.Infrastructure.Authentication;

namespace EcoBilling.IntegrationTests.Controllers;

public sealed class ControllerCreationRequestFingerprinterTests
{
    private const string Key =
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

    [Fact]
    public void Create_IsDeterministicSensitiveAndDoesNotExposeCredential()
    {
        var fingerprinter = new ControllerCreationRequestFingerprinter(Key);

        var first = fingerprinter.Create(
            "Grace Hopper",
            "CONTROLLER@EXAMPLE.COM",
            "initial-credential-0123456789012345");
        var repeated = fingerprinter.Create(
            "Grace Hopper",
            "CONTROLLER@EXAMPLE.COM",
            "initial-credential-0123456789012345");
        var changed = fingerprinter.Create(
            "Grace Hopper",
            "CONTROLLER@EXAMPLE.COM",
            "different-credential-01234567890123");

        Assert.Equal(first, repeated);
        Assert.NotEqual(first, changed);
        Assert.Equal(64, first.Length);
        Assert.DoesNotContain(
            "initial-credential",
            first,
            StringComparison.OrdinalIgnoreCase);
    }
}
