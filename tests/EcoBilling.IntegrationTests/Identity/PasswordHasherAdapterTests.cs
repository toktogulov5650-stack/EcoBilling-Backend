using EcoBilling.Infrastructure.Authentication;
using EcoBilling.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EcoBilling.IntegrationTests.Identity;

public sealed class PasswordHasherAdapterTests
{
    private readonly PasswordHasherAdapter sut = new();

    [Fact]
    public void Hash_ThenVerify_ReturnsSuccessWithoutStoringPlaintext()
    {
        const string Password = "correct horse battery staple";

        var hash = sut.Hash(Password);

        Assert.NotEqual(Password, hash);
        Assert.DoesNotContain(Password, hash, StringComparison.Ordinal);
        Assert.Equal(PasswordVerificationOutcome.Success, sut.Verify(Password, hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFailed()
    {
        var hash = sut.Hash("correct-password");

        var outcome = sut.Verify("wrong-password", hash);

        Assert.Equal(PasswordVerificationOutcome.Failed, outcome);
    }

    [Fact]
    public void Verify_WithIdentityV2Hash_ReturnsSuccessRehashNeeded()
    {
        const string Password = "legacy-password";
        var legacyHasher = new PasswordHasher<object>(
            Options.Create(
                new PasswordHasherOptions
                {
                    CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2
                }));
        var legacyHash = legacyHasher.HashPassword(new object(), Password);

        var outcome = sut.Verify(Password, legacyHash);

        Assert.Equal(PasswordVerificationOutcome.SuccessRehashNeeded, outcome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Hash_WithMissingPassword_Throws(string? password)
    {
        Assert.ThrowsAny<ArgumentException>(() => sut.Hash(password!));
    }

    [Theory]
    [InlineData(null, "hash")]
    [InlineData("", "hash")]
    [InlineData("password", null)]
    [InlineData("password", "")]
    public void Verify_WithMissingInput_Throws(string? password, string? hash)
    {
        Assert.ThrowsAny<ArgumentException>(() => sut.Verify(password!, hash!));
    }
}
