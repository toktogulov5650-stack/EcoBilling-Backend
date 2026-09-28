using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.UnitTests.Residents.Domain;

public sealed class ResidentPasswordResetOperationTests
{
    private const string Fingerprint =
        "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public void Create_WithValidValues_NormalizesFingerprintAndTimestamp()
    {
        var createdAt = new DateTimeOffset(
            2026,
            9,
            29,
            12,
            0,
            0,
            TimeSpan.FromHours(6));
        var residentId = new ResidentId(Guid.NewGuid());

        var result = ResidentPasswordResetOperation.Create(
            new ResidentPasswordResetOperationId(Guid.NewGuid()),
            "reset-resident-password-1",
            Fingerprint.ToLowerInvariant(),
            residentId,
            createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal("reset-resident-password-1", result.Value.IdempotencyKey);
        Assert.Equal(Fingerprint, result.Value.RequestFingerprint);
        Assert.Equal(residentId, result.Value.ResidentId);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidIdempotencyKey_ReturnsFailure(string? key)
    {
        var result = Create(key, Fingerprint);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ResidentErrors.InvalidPasswordResetIdempotencyKey,
            result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-hex")]
    public void Create_WithInvalidFingerprint_ReturnsFailure(string? fingerprint)
    {
        var result = Create("reset-resident-password-1", fingerprint);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ResidentErrors.InvalidPasswordResetRequestFingerprint,
            result.Error);
    }

    private static EcoBilling.SharedKernel.Results.Result<ResidentPasswordResetOperation>
        Create(string? idempotencyKey, string? fingerprint) =>
        ResidentPasswordResetOperation.Create(
            new ResidentPasswordResetOperationId(Guid.NewGuid()),
            idempotencyKey,
            fingerprint,
            new ResidentId(Guid.NewGuid()),
            DateTimeOffset.UtcNow);
}
