using EcoBilling.Infrastructure.Authentication;

namespace EcoBilling.IntegrationTests.Identity;

public sealed class DirectorProvisioningRequestFingerprinterTests
{
    private const string Key =
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

    [Fact]
    public void Create_IsDeterministicAndDoesNotExposeCredential()
    {
        const string credential =
            "xTOHxQm8oC24bTWf9Uh5AF0w9Jm1mSIKzRFMtAlSHXo";
        var fingerprinter = new DirectorProvisioningRequestFingerprinter(Key);

        var first = fingerprinter.Create(
            "Ada Lovelace",
            "DIRECTOR@EXAMPLE.COM",
            credential);
        var second = fingerprinter.Create(
            "Ada Lovelace",
            "DIRECTOR@EXAMPLE.COM",
            credential);

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
        Assert.DoesNotContain(credential, first, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WhenCredentialChanges_ChangesFingerprint()
    {
        var fingerprinter = new DirectorProvisioningRequestFingerprinter(Key);

        var first = fingerprinter.Create(
            "Ada Lovelace",
            "DIRECTOR@EXAMPLE.COM",
            new string('A', 32));
        var second = fingerprinter.Create(
            "Ada Lovelace",
            "DIRECTOR@EXAMPLE.COM",
            new string('B', 32));

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("c2hvcnQ=")]
    public void Constructor_WithInvalidKey_Throws(string key)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new DirectorProvisioningRequestFingerprinter(key));
    }
}
