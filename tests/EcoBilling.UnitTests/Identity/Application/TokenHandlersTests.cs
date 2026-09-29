using EcoBilling.Modules.Identity.Application;
using EcoBilling.Modules.Identity.Application.Abstractions;
using EcoBilling.Modules.Identity.Application.Authenticate;
using EcoBilling.Modules.Identity.Application.Tokens;
using EcoBilling.Modules.Identity.Contracts;
using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.UnitTests.Identity.Application;

public sealed class TokenHandlersTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 9, 28, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Login_CreatesRefreshSessionAndReturnsRedactedTokenPair()
    {
        var account = CreateAccount();
        var sessions = new RecordingRefreshSessionRepository();
        var handler = new LoginHandler(
            CreateAuthenticateHandler(account),
            new StubAccessTokenIssuer(),
            new StubRefreshTokenService(),
            sessions,
            TokenPolicy.Default,
            new FixedTimeProvider(UtcNow));

        var result = await handler.Handle(
            new AuthenticateCommand(LoginType.Email, "controller@example.com", "valid-password"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("refresh-token-1", result.Value.RefreshToken);
        Assert.Equal(UtcNow.AddDays(30), result.Value.RefreshTokenExpiresAt);
        Assert.NotNull(sessions.Created);
        Assert.Equal(account.Id, sessions.Created.UserId);
        Assert.Equal(new string('1', 64), sessions.Created.TokenHash);
        Assert.DoesNotContain("access-token", result.Value.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("refresh-token-1", result.Value.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refresh_RotatesSessionAndReturnsTokensForPersistedUser()
    {
        var userId = new UserId(Guid.NewGuid());
        var sessions = new RecordingRefreshSessionRepository
        {
            RotationResult = new RefreshSessionRotationResult(
                RefreshSessionRotationOutcome.Rotated,
                userId,
                UserRole.Resident)
        };
        var handler = new RefreshHandler(
            new StubAccessTokenIssuer(),
            new StubRefreshTokenService(),
            sessions,
            TokenPolicy.Default,
            new FixedTimeProvider(UtcNow));

        var result = await handler.Handle("presented-token", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.User.UserId);
        Assert.Equal(UserRole.Resident, result.Value.User.Role);
        Assert.Equal("hash:presented-token", sessions.ReceivedCurrentHash);
        Assert.Equal(new string('1', 64), sessions.ReceivedReplacementHash);
        Assert.Equal(UtcNow.AddDays(30), sessions.ReceivedReplacementExpiresAt);
    }

    [Theory]
    [InlineData(RefreshSessionRotationOutcome.Invalid)]
    [InlineData(RefreshSessionRotationOutcome.Reused)]
    public async Task Refresh_HidesInvalidAndReusedSession(
        RefreshSessionRotationOutcome outcome)
    {
        var sessions = new RecordingRefreshSessionRepository
        {
            RotationResult = new RefreshSessionRotationResult(outcome)
        };
        var handler = new RefreshHandler(
            new StubAccessTokenIssuer(),
            new StubRefreshTokenService(),
            sessions,
            TokenPolicy.Default,
            new FixedTimeProvider(UtcNow));

        var result = await handler.Handle("presented-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidRefreshToken, result.Error);
    }

    [Fact]
    public async Task RevokeAllForUser_UsesActorCorrelationAndCurrentTime()
    {
        var userId = new UserId(Guid.NewGuid());
        var sessions = new RecordingRefreshSessionRepository
        {
            UserRevocationResult = new UserSessionRevocationResult(
                UserSessionRevocationOutcome.Revoked,
                3)
        };
        var handler = new RevokeUserSessionsHandler(
            sessions,
            new FixedTimeProvider(UtcNow));

        var result = await handler.Handle(
            new RevokeUserSessionsCommand(
                userId,
                "director-user-id",
                "trace-123"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value.UserId);
        Assert.Equal(3, result.Value.RevokedSessions);
        Assert.Equal(userId, sessions.ReceivedRevokedUserId);
        Assert.Equal("director-user-id", sessions.ReceivedActorId);
        Assert.Equal("trace-123", sessions.ReceivedCorrelationId);
        Assert.Equal(UtcNow, sessions.ReceivedRevokeAllTime);
    }

    [Fact]
    public async Task RevokeAllForUser_HidesMissingUserBehindNotFoundError()
    {
        var sessions = new RecordingRefreshSessionRepository
        {
            UserRevocationResult = new UserSessionRevocationResult(
                UserSessionRevocationOutcome.UserNotFound)
        };
        var handler = new RevokeUserSessionsHandler(
            sessions,
            new FixedTimeProvider(UtcNow));

        var result = await handler.Handle(
            new RevokeUserSessionsCommand(
                new UserId(Guid.NewGuid()),
                "director-user-id",
                "trace-123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Revoke_HashesPresentedTokenAndIsSilentForEmptyValue()
    {
        var sessions = new RecordingRefreshSessionRepository();
        var handler = new RevokeRefreshTokenHandler(
            new StubRefreshTokenService(),
            sessions,
            new FixedTimeProvider(UtcNow));

        await handler.Handle(null, CancellationToken.None);
        await handler.Handle("presented-token", CancellationToken.None);

        Assert.Equal(1, sessions.RevokeCount);
        Assert.Equal("hash:presented-token", sessions.ReceivedRevocationHash);
        Assert.Equal(UtcNow, sessions.ReceivedRevocationTime);
    }

    private static AuthenticateHandler CreateAuthenticateHandler(UserAccount account) =>
        new(
            new StubUserAccountRepository(account),
            new StubPasswordHasher(),
            new LoginNormalizer(),
            AuthenticationPolicy.Default,
            new FixedTimeProvider(UtcNow));

    private static UserAccount CreateAccount() =>
        UserAccount.Create(
            new UserId(Guid.NewGuid()),
            LoginIdentity.Create(LoginType.Email, "controller@example.com").Value,
            "stored-hash",
            UserRole.Controller,
            UtcNow).Value;

    private sealed class StubUserAccountRepository(UserAccount account)
        : IUserAccountRepository
    {
        public Task<UserAccount?> GetByLoginAsync(
            LoginIdentity loginIdentity,
            CancellationToken cancellationToken) => Task.FromResult<UserAccount?>(account);

        public Task SaveAsync(
            UserAccount userAccount,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "stored-hash";

        public PasswordVerificationOutcome Verify(string password, string passwordHash) =>
            password == "valid-password" && passwordHash == "stored-hash"
                ? PasswordVerificationOutcome.Success
                : PasswordVerificationOutcome.Failed;
    }

    private sealed class StubAccessTokenIssuer : IAccessTokenIssuer
    {
        public IssuedAccessToken Issue(AuthenticatedUser user, DateTimeOffset issuedAt) =>
            new("access-token", issuedAt.AddMinutes(15));
    }

    private sealed class StubRefreshTokenService : IRefreshTokenService
    {
        public GeneratedRefreshToken Generate() =>
            new("refresh-token-1", new string('1', 64));

        public string Hash(string token) => $"hash:{token}";
    }

    private sealed class RecordingRefreshSessionRepository : IRefreshSessionRepository
    {
        public RefreshSession? Created { get; private set; }

        public RefreshSessionRotationResult RotationResult { get; init; } =
            new(RefreshSessionRotationOutcome.Invalid);

        public string? ReceivedCurrentHash { get; private set; }

        public string? ReceivedReplacementHash { get; private set; }

        public DateTimeOffset ReceivedReplacementExpiresAt { get; private set; }

        public int RevokeCount { get; private set; }

        public string? ReceivedRevocationHash { get; private set; }

        public DateTimeOffset ReceivedRevocationTime { get; private set; }

        public UserSessionRevocationResult UserRevocationResult { get; init; } =
            new(UserSessionRevocationOutcome.Revoked);

        public UserId? ReceivedRevokedUserId { get; private set; }

        public string? ReceivedActorId { get; private set; }

        public string? ReceivedCorrelationId { get; private set; }

        public DateTimeOffset ReceivedRevokeAllTime { get; private set; }

        public Task CreateAsync(RefreshSession session, CancellationToken cancellationToken)
        {
            Created = session;
            return Task.CompletedTask;
        }

        public Task<RefreshSessionRotationResult> RotateAsync(
            string currentTokenHash,
            RefreshSessionId replacementId,
            string replacementTokenHash,
            DateTimeOffset now,
            DateTimeOffset replacementExpiresAt,
            CancellationToken cancellationToken)
        {
            ReceivedCurrentHash = currentTokenHash;
            ReceivedReplacementHash = replacementTokenHash;
            ReceivedReplacementExpiresAt = replacementExpiresAt;
            return Task.FromResult(RotationResult);
        }

        public Task RevokeAsync(
            string tokenHash,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            RevokeCount++;
            ReceivedRevocationHash = tokenHash;
            ReceivedRevocationTime = now;
            return Task.CompletedTask;
        }

        public Task<UserSessionRevocationResult> RevokeAllForUserAsync(
            UserId userId,
            string actorId,
            string correlationId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ReceivedRevokedUserId = userId;
            ReceivedActorId = actorId;
            ReceivedCorrelationId = correlationId;
            ReceivedRevokeAllTime = now;
            return Task.FromResult(UserRevocationResult);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
