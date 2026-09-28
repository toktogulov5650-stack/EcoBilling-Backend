using EcoBilling.Modules.Accounts.Domain;
using EcoBilling.Modules.Residents.Domain;

namespace EcoBilling.UnitTests.Residents.Domain;

public sealed class ResidentCreationOperationTests
{
    private const string Fingerprint =
        "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public void Create_WithValidValues_NormalizesFingerprintAndTimestamp()
    {
        var createdAt = new DateTimeOffset(
            2026,
            9,
            28,
            18,
            0,
            0,
            TimeSpan.FromHours(6));

        var result = ResidentCreationOperation.Create(
            new ResidentCreationOperationId(Guid.NewGuid()),
            "create-resident-1",
            Fingerprint.ToLowerInvariant(),
            new ResidentId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            new AddressId(Guid.NewGuid()),
            createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal("create-resident-1", result.Value.IdempotencyKey);
        Assert.Equal(Fingerprint, result.Value.RequestFingerprint);
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
        Assert.Equal(ResidentErrors.InvalidCreationIdempotencyKey, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-hex")]
    public void Create_WithInvalidFingerprint_ReturnsFailure(string? fingerprint)
    {
        var result = Create("create-resident-1", fingerprint);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ResidentErrors.InvalidCreationRequestFingerprint,
            result.Error);
    }

    private static EcoBilling.SharedKernel.Results.Result<ResidentCreationOperation> Create(
        string? idempotencyKey,
        string? fingerprint) =>
        ResidentCreationOperation.Create(
            new ResidentCreationOperationId(Guid.NewGuid()),
            idempotencyKey,
            fingerprint,
            new ResidentId(Guid.NewGuid()),
            new AccountId(Guid.NewGuid()),
            new AddressId(Guid.NewGuid()),
            DateTimeOffset.UtcNow);
}
