using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Domain;

public sealed class DirectorProvisioningOperationTests
{
    private const string Fingerprint =
        "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Fact]
    public void Create_WithValidData_PreservesOpaqueIdempotencyKey()
    {
        const string idempotencyKey = " Provision-Director-AbC-123 ";
        var createdAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(6));

        var result = DirectorProvisioningOperation.Create(
            new DirectorProvisioningOperationId(Guid.NewGuid()),
            idempotencyKey,
            Fingerprint.ToLowerInvariant(),
            new DirectorId(Guid.NewGuid()),
            createdAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(idempotencyKey, result.Value.IdempotencyKey);
        Assert.Equal(Fingerprint, result.Value.RequestFingerprint);
        Assert.Equal(createdAt.ToUniversalTime(), result.Value.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutIdempotencyKey_ReturnsValidationError(string? key)
    {
        var result = Create(key, Fingerprint);

        Assert.True(result.IsFailure);
        Assert.Equal(DirectorProvisioningErrors.InvalidIdempotencyKey, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG")]
    public void Create_WithInvalidFingerprint_ReturnsValidationError(string? fingerprint)
    {
        var result = Create("operation-1", fingerprint);

        Assert.True(result.IsFailure);
        Assert.Equal(DirectorProvisioningErrors.InvalidRequestFingerprint, result.Error);
    }

    private static EcoBilling.SharedKernel.Results.Result<DirectorProvisioningOperation> Create(
        string? key,
        string? fingerprint) =>
        DirectorProvisioningOperation.Create(
            new DirectorProvisioningOperationId(Guid.NewGuid()),
            key,
            fingerprint,
            new DirectorId(Guid.NewGuid()),
            DateTimeOffset.UtcNow);
}
