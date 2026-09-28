using EcoBilling.Modules.Controllers.Domain;

namespace EcoBilling.UnitTests.Controllers.Domain;

public sealed class ControllerCreationOperationTests
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

        var result = ControllerCreationOperation.Create(
            new ControllerCreationOperationId(Guid.NewGuid()),
            "create-controller-1",
            Fingerprint.ToLowerInvariant(),
            new ControllerId(Guid.NewGuid()),
            createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal("create-controller-1", result.Value.IdempotencyKey);
        Assert.Equal(Fingerprint, result.Value.RequestFingerprint);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidIdempotencyKey_ReturnsFailure(string? key)
    {
        var result = ControllerCreationOperation.Create(
            new ControllerCreationOperationId(Guid.NewGuid()),
            key,
            Fingerprint,
            new ControllerId(Guid.NewGuid()),
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ControllerErrors.InvalidCreationIdempotencyKey, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-hex")]
    public void Create_WithInvalidFingerprint_ReturnsFailure(string? fingerprint)
    {
        var result = ControllerCreationOperation.Create(
            new ControllerCreationOperationId(Guid.NewGuid()),
            "create-controller-1",
            fingerprint,
            new ControllerId(Guid.NewGuid()),
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ControllerErrors.InvalidCreationRequestFingerprint,
            result.Error);
    }
}
