using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcoBilling.Api.Endpoints;
using EcoBilling.EndToEndTests.Infrastructure;
using EcoBilling.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.EndToEndTests.Journeys;

public sealed class ControllerCreationJourneyTests
{
    private const string DirectorPassword = "Director-password-2026!";
    private const string ControllerInitialCredential =
        "controller-initial-credential-2026-01";
    private const string ControllerPassword = "Controller-password-2026!";

    [PostgreSqlEndToEndFact]
    public async Task Director_CreatesControllerWhoCompletesPasswordSetupAndLogsIn()
    {
        await using var host = await EcoBillingEndToEndHost.CreateAsync();
        await ProvisionDirectorAsync(host);
        await SetupPasswordAsync(
            host.Client,
            "director@example.com",
            EcoBillingEndToEndHost.InitialCredential,
            DirectorPassword);
        var directorTokens = await LoginAsync(
            host.Client,
            "director@example.com",
            DirectorPassword);

        using var createRequest = CreateControllerRequest(
            directorTokens.AccessToken,
            "create-controller-1");
        createRequest.Headers.Add("X-Correlation-Id", "e2e-controller-trace-1");
        using var createResponse = await host.Client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(
            "false",
            createResponse.Headers.GetValues("Idempotency-Replayed").Single());
        var created = await createResponse.Content
            .ReadFromJsonAsync<CreateControllerResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.ControllerId);
        Assert.NotEqual(Guid.Empty, created.OperationId);
        Assert.DoesNotContain(
            ControllerInitialCredential,
            await createResponse.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);

        using var replayRequest = CreateControllerRequest(
            directorTokens.AccessToken,
            "create-controller-1");
        using var replayResponse = await host.Client.SendAsync(replayRequest);
        Assert.Equal(HttpStatusCode.Created, replayResponse.StatusCode);
        Assert.Equal(
            "true",
            replayResponse.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal(
            created,
            await replayResponse.Content.ReadFromJsonAsync<CreateControllerResponse>());

        using var loginBeforeSetupResponse = await PostLoginAsync(
            host.Client,
            "controller@example.com",
            ControllerInitialCredential);
        Assert.Equal(HttpStatusCode.Forbidden, loginBeforeSetupResponse.StatusCode);

        await SetupPasswordAsync(
            host.Client,
            "controller@example.com",
            ControllerInitialCredential,
            ControllerPassword);
        var controllerTokens = await LoginAsync(
            host.Client,
            "controller@example.com",
            ControllerPassword);
        Assert.Equal(UserRole.Controller.ToString(), controllerTokens.Role);

        await using var context = host.CreateContext();
        var controller = await context.Controllers.AsNoTracking().SingleAsync();
        var controllerAccount = await context.UserAccounts
            .AsNoTracking()
            .SingleAsync(account => account.Role == UserRole.Controller);
        var operation = await context.ControllerCreationOperations
            .AsNoTracking()
            .SingleAsync();
        var audit = await context.AuditLogs
            .AsNoTracking()
            .SingleAsync(log => log.Action == "controllers.controller.created");
        var directorAccount = await context.UserAccounts
            .AsNoTracking()
            .SingleAsync(account => account.Role == UserRole.Director);

        Assert.Equal(created.ControllerId, controller.Id.Value);
        Assert.Equal(controllerAccount.Id, controller.UserId);
        Assert.Equal(created.OperationId, operation.Id.Value);
        Assert.Equal(controller.Id, operation.ControllerId);
        Assert.False(controllerAccount.RequiresPasswordChange);
        Assert.NotEqual(ControllerPassword, controllerAccount.PasswordHash);
        Assert.Equal(directorAccount.Id.Value.ToString("D"), audit.ActorId);
        Assert.Equal("e2e-controller-trace-1", audit.CorrelationId);
        Assert.DoesNotContain(
            ControllerInitialCredential,
            audit.AfterData ?? string.Empty,
            StringComparison.Ordinal);
    }

    private static async Task ProvisionDirectorAsync(EcoBillingEndToEndHost host)
    {
        using var request = host.CreateDirectorRequest(
            "provision-director-for-controller-journey",
            host.CreateServiceToken("controller-journey-token-1"));
        using var response = await host.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task SetupPasswordAsync(
        HttpClient client,
        string email,
        string initialCredential,
        string newPassword)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/setup-password",
            new SetInitialPasswordRequest(
                "email",
                email,
                initialCredential,
                newPassword));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<TokenPairResponse> LoginAsync(
        HttpClient client,
        string email,
        string password)
    {
        using var response = await PostLoginAsync(client, email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<TokenPairResponse>();
        return Assert.IsType<TokenPairResponse>(tokens);
    }

    private static Task<HttpResponseMessage> PostLoginAsync(
        HttpClient client,
        string email,
        string password) =>
        client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("email", email, password));

    private static HttpRequestMessage CreateControllerRequest(
        string accessToken,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/controllers")
        {
            Content = JsonContent.Create(
                new CreateControllerRequest(
                    "Grace Hopper",
                    "controller@example.com",
                    ControllerInitialCredential))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);
        request.Headers.Add(
            ControllerManagementEndpoints.IdempotencyKeyHeaderName,
            idempotencyKey);
        return request;
    }
}
