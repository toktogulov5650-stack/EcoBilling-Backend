using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcoBilling.Api.Endpoints;
using EcoBilling.EndToEndTests.Infrastructure;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.EndToEndTests.Journeys;

public sealed class ResidentCreationJourneyTests
{
    private const string DirectorPassword = "Director-password-2026!";
    private const string ResidentPassword = "Resident-password-2026!";
    private const string ReplacementResidentPassword =
        "Replacement-resident-password-2026!";
    private const string AccountNumber = "AB-000001";

    [PostgreSqlEndToEndFact]
    public async Task Director_CreatesResidentWhoLogsInByAccountNumber()
    {
        await using var host = await EcoBillingEndToEndHost.CreateAsync();
        await ProvisionDirectorAsync(host);
        await SetupDirectorPasswordAsync(host.Client);
        var directorTokens = await LoginAsync(
            host.Client,
            "email",
            "director@example.com",
            DirectorPassword);

        using var createRequest = CreateResidentRequest(
            directorTokens.AccessToken,
            "create-resident-1");
        createRequest.Headers.Add("X-Correlation-Id", "e2e-resident-trace-1");
        using var createResponse = await host.Client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(
            "false",
            createResponse.Headers.GetValues("Idempotency-Replayed").Single());
        var created = await createResponse.Content
            .ReadFromJsonAsync<CreateResidentResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.ResidentId);
        Assert.NotEqual(Guid.Empty, created.AccountId);
        Assert.NotEqual(Guid.Empty, created.AddressId);
        Assert.NotEqual(Guid.Empty, created.OperationId);
        Assert.DoesNotContain(
            ResidentPassword,
            await createResponse.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);

        using var replayRequest = CreateResidentRequest(
            directorTokens.AccessToken,
            "create-resident-1");
        using var replayResponse = await host.Client.SendAsync(replayRequest);
        Assert.Equal(HttpStatusCode.Created, replayResponse.StatusCode);
        Assert.Equal(
            "true",
            replayResponse.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal(
            created,
            await replayResponse.Content.ReadFromJsonAsync<CreateResidentResponse>());

        var residentTokens = await LoginAsync(
            host.Client,
            "accountNumber",
            " ab-000001 ",
            ResidentPassword);
        Assert.Equal(UserRole.Resident.ToString(), residentTokens.Role);

        using var resetRequest = ResetResidentPasswordRequest(
            directorTokens.AccessToken,
            created.ResidentId,
            "reset-resident-password-1");
        resetRequest.Headers.Add(
            "X-Correlation-Id",
            "e2e-resident-password-reset-trace-1");
        using var resetResponse = await host.Client.SendAsync(resetRequest);
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);
        Assert.Equal(
            "false",
            resetResponse.Headers.GetValues("Idempotency-Replayed").Single());
        var reset = await resetResponse.Content
            .ReadFromJsonAsync<ResetResidentPasswordResponse>();
        Assert.NotNull(reset);
        Assert.NotEqual(Guid.Empty, reset.OperationId);
        Assert.DoesNotContain(
            ReplacementResidentPassword,
            await resetResponse.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);

        using var resetReplayRequest = ResetResidentPasswordRequest(
            directorTokens.AccessToken,
            created.ResidentId,
            "reset-resident-password-1");
        using var resetReplayResponse = await host.Client.SendAsync(resetReplayRequest);
        Assert.Equal(HttpStatusCode.OK, resetReplayResponse.StatusCode);
        Assert.Equal(
            "true",
            resetReplayResponse.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal(
            reset,
            await resetReplayResponse.Content
                .ReadFromJsonAsync<ResetResidentPasswordResponse>());

        using var refreshAfterResetResponse = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(residentTokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterResetResponse.StatusCode);

        using var oldPasswordLoginResponse = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("accountNumber", AccountNumber, ResidentPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLoginResponse.StatusCode);

        var replacementTokens = await LoginAsync(
            host.Client,
            "accountNumber",
            AccountNumber,
            ReplacementResidentPassword);
        Assert.Equal(UserRole.Resident.ToString(), replacementTokens.Role);

        await using var context = host.CreateContext();
        var resident = await context.Residents.AsNoTracking().SingleAsync();
        var residentIdentity = await context.UserAccounts
            .AsNoTracking()
            .SingleAsync(account => account.Role == UserRole.Resident);
        var account = await context.Accounts.AsNoTracking().SingleAsync();
        var address = await context.Addresses.AsNoTracking().SingleAsync();
        var operation = await context.ResidentCreationOperations
            .AsNoTracking()
            .SingleAsync();
        var resetOperation = await context.ResidentPasswordResetOperations
            .AsNoTracking()
            .SingleAsync();
        var audit = await context.AuditLogs
            .AsNoTracking()
            .SingleAsync(log => log.Action == "residents.resident.created");
        var directorIdentity = await context.UserAccounts
            .AsNoTracking()
            .SingleAsync(account => account.Role == UserRole.Director);
        var resetAudit = await context.AuditLogs
            .AsNoTracking()
            .SingleAsync(log => log.Action == "residents.resident.password_reset");

        Assert.Equal(created.ResidentId, resident.Id.Value);
        Assert.Equal(residentIdentity.Id, resident.UserId);
        Assert.Equal(created.AccountId, account.Id.Value);
        Assert.Equal(resident.Id, account.ResidentId);
        Assert.Equal(created.AddressId, address.Id.Value);
        Assert.Equal(address.Id, account.AddressId);
        Assert.Equal(AccountNumber, account.Number.Value);
        Assert.Equal(created.OperationId, operation.Id.Value);
        Assert.Equal(reset.OperationId, resetOperation.Id.Value);
        Assert.Equal(resident.Id, resetOperation.ResidentId);
        Assert.Equal(resident.Id, operation.ResidentId);
        Assert.Equal(account.Id, operation.AccountId);
        Assert.Equal(address.Id, operation.AddressId);
        Assert.False(residentIdentity.RequiresPasswordChange);
        Assert.NotEqual(ResidentPassword, residentIdentity.PasswordHash);
        Assert.NotEqual(ReplacementResidentPassword, residentIdentity.PasswordHash);
        Assert.Equal(directorIdentity.Id.Value.ToString("D"), audit.ActorId);
        Assert.Equal("e2e-resident-trace-1", audit.CorrelationId);
        Assert.DoesNotContain(
            ResidentPassword,
            audit.AfterData ?? string.Empty,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            AccountNumber,
            audit.AfterData ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(directorIdentity.Id.Value.ToString("D"), resetAudit.ActorId);
        Assert.Equal(
            "e2e-resident-password-reset-trace-1",
            resetAudit.CorrelationId);
        Assert.DoesNotContain(
            ReplacementResidentPassword,
            resetAudit.AfterData ?? string.Empty,
            StringComparison.Ordinal);
    }

    private static async Task ProvisionDirectorAsync(EcoBillingEndToEndHost host)
    {
        using var request = host.CreateDirectorRequest(
            "provision-director-for-resident-journey",
            host.CreateServiceToken("resident-journey-token-1"));
        using var response = await host.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task SetupDirectorPasswordAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/setup-password",
            new SetInitialPasswordRequest(
                "email",
                "director@example.com",
                EcoBillingEndToEndHost.InitialCredential,
                DirectorPassword));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<TokenPairResponse> LoginAsync(
        HttpClient client,
        string loginType,
        string login,
        string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(loginType, login, password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<TokenPairResponse>();
        return Assert.IsType<TokenPairResponse>(tokens);
    }

    private static HttpRequestMessage CreateResidentRequest(
        string accessToken,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/residents")
        {
            Content = JsonContent.Create(
                new CreateResidentRequest(
                    "Ada Lovelace",
                    AccountNumber,
                    ResidentPassword,
                    new CreateResidentAddressRequest(
                        "Bishkek",
                        "Chuy Avenue",
                        "42",
                        "2",
                        "17")))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);
        request.Headers.Add(
            ResidentManagementEndpoints.IdempotencyKeyHeaderName,
            idempotencyKey);
        return request;
    }

    private static HttpRequestMessage ResetResidentPasswordRequest(
        string accessToken,
        Guid residentId,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/residents/{residentId:D}/password")
        {
            Content = JsonContent.Create(
                new ResetResidentPasswordRequest(ReplacementResidentPassword))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);
        request.Headers.Add(
            ResidentManagementEndpoints.IdempotencyKeyHeaderName,
            idempotencyKey);
        return request;
    }
}
