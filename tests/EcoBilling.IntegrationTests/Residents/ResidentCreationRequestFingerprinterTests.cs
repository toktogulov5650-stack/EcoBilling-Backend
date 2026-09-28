using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.IntegrationTests.Residents;

public sealed class ResidentCreationRequestFingerprinterTests
{
    private const string Key =
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

    [Fact]
    public void Create_IsDeterministicSensitiveAndDoesNotExposeSecrets()
    {
        var fingerprinter = new ResidentCreationRequestFingerprinter(Key);
        var address = Address.Create(
            new AddressId(Guid.NewGuid()),
            "Bishkek",
            "Chuy Avenue",
            "42",
            "2",
            "17").Value;

        var first = fingerprinter.Create(
            "Ada Lovelace",
            "AB-000001",
            "resident-password-0123456789",
            address);
        var repeated = fingerprinter.Create(
            "Ada Lovelace",
            "AB-000001",
            "resident-password-0123456789",
            address);
        var changed = fingerprinter.Create(
            "Ada Lovelace",
            "AB-000001",
            "different-password-0123456789",
            address);

        Assert.Equal(first, repeated);
        Assert.NotEqual(first, changed);
        Assert.Equal(64, first.Length);
        Assert.DoesNotContain("resident-password", first, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AB-000001", first, StringComparison.OrdinalIgnoreCase);
    }
}
