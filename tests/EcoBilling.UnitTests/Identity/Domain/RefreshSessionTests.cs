using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Domain;

public sealed class RefreshSessionTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 28, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_NormalizesHashAndStoresUtcTimes()
    {
        var session = CreateSession(new string('a', RefreshSession.TokenHashLength));

        Assert.Equal(new string('A', RefreshSession.TokenHashLength), session.TokenHash);
        Assert.Equal(TimeSpan.Zero, session.CreatedAt.Offset);
        Assert.Equal(TimeSpan.Zero, session.ExpiresAt.Offset);
        Assert.True(session.IsActive(CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
    public void Create_RejectsInvalidTokenHash(string tokenHash)
    {
        var result = RefreshSession.Create(
            new RefreshSessionId(Guid.NewGuid()),
            new UserId(Guid.NewGuid()),
            Guid.NewGuid(),
            tokenHash,
            CreatedAt,
            CreatedAt.AddDays(30));

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidRefreshToken, result.Error);
    }

    [Fact]
    public void Consume_MakesSessionInactiveAndLinksReplacement()
    {
        var session = CreateSession(new string('A', RefreshSession.TokenHashLength));
        var replacementId = new RefreshSessionId(Guid.NewGuid());

        session.Consume(CreatedAt.AddMinutes(1), replacementId);

        Assert.False(session.IsActive(CreatedAt.AddMinutes(1)));
        Assert.Equal(CreatedAt.AddMinutes(1), session.ConsumedAt);
        Assert.Equal(replacementId, session.ReplacedBySessionId);
        Assert.Throws<InvalidOperationException>(
            () => session.Consume(CreatedAt.AddMinutes(2), new RefreshSessionId(Guid.NewGuid())));
    }

    [Fact]
    public void Revoke_IsIdempotentAndKeepsOriginalTimestamp()
    {
        var session = CreateSession(new string('B', RefreshSession.TokenHashLength));
        session.Revoke(CreatedAt.AddMinutes(1));

        session.Revoke(CreatedAt.AddMinutes(2));

        Assert.Equal(CreatedAt.AddMinutes(1), session.RevokedAt);
        Assert.False(session.IsActive(CreatedAt.AddMinutes(1)));
    }

    private static RefreshSession CreateSession(string tokenHash) =>
        RefreshSession.Create(
            new RefreshSessionId(Guid.NewGuid()),
            new UserId(Guid.NewGuid()),
            Guid.NewGuid(),
            tokenHash,
            CreatedAt,
            CreatedAt.AddDays(30)).Value;
}
