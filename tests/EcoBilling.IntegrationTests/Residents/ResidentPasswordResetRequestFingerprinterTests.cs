using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.IntegrationTests.Residents;

public sealed class ResidentPasswordResetRequestFingerprinterTests
{
    private const string Key =
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

    [Fact]
    public void Create_IsDeterministicSensitiveAndDoesNotExposePassword()
    {
        var fingerprinter = new ResidentPasswordResetRequestFingerprinter(Key);
        var residentId = new ResidentId(Guid.NewGuid());

        var first = fingerprinter.Create(
            residentId,
            "new-resident-password-2026!");
        var repeated = fingerprinter.Create(
            residentId,
            "new-resident-password-2026!");
        var changedPassword = fingerprinter.Create(
            residentId,
            "different-resident-password-2026!");
        var changedResident = fingerprinter.Create(
            new ResidentId(Guid.NewGuid()),
            "new-resident-password-2026!");

        Assert.Equal(first, repeated);
        Assert.NotEqual(first, changedPassword);
        Assert.NotEqual(first, changedResident);
        Assert.Equal(64, first.Length);
        Assert.DoesNotContain(
            "resident-password",
            first,
            StringComparison.OrdinalIgnoreCase);
    }
}
