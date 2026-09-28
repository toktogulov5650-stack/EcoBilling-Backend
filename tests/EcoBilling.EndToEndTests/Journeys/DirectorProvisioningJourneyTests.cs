using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EcoBilling.Api.InternalEndpoints;
using EcoBilling.EndToEndTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EcoBilling.EndToEndTests.Journeys;

public sealed class DirectorProvisioningJourneyTests
{
    [PostgreSqlEndToEndFact]
    public async Task Control_AuthenticatesCreatesDirectorAndSafelyRetriesOperation()
    {
        await using var host = await EcoBillingEndToEndHost.CreateAsync();
        using var unauthorizedRequest = host.CreateDirectorRequest(
            "operation-1",
            token: null);
        using var unauthorizedResponse = await host.Client.SendAsync(
            unauthorizedRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
        await AssertProblemCodeAsync(
            unauthorizedResponse,
            "service.unauthorized");

        using var createRequest = host.CreateDirectorRequest(
            "operation-1",
            host.CreateServiceToken("token-1"));
        createRequest.Headers.Add("X-Correlation-Id", "e2e-control-trace-1");
        using var createResponse = await host.Client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(
            "false",
            createResponse.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.Equal(
            "e2e-control-trace-1",
            createResponse.Headers.GetValues("X-Correlation-Id").Single());
        var created = await createResponse.Content
            .ReadFromJsonAsync<ProvisionDirectorResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.DirectorId);
        Assert.NotEqual(Guid.Empty, created.OperationId);
        Assert.Equal("created", created.Status);
        Assert.DoesNotContain(
            EcoBillingEndToEndHost.InitialCredential,
            await createResponse.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);

        using var retryRequest = host.CreateDirectorRequest(
            "operation-1",
            host.CreateServiceToken("token-2"));
        using var retryResponse = await host.Client.SendAsync(retryRequest);

        Assert.Equal(HttpStatusCode.Created, retryResponse.StatusCode);
        Assert.Equal(
            "true",
            retryResponse.Headers.GetValues("Idempotency-Replayed").Single());
        var replayed = await retryResponse.Content
            .ReadFromJsonAsync<ProvisionDirectorResponse>();
        Assert.Equal(created, replayed);

        await using var context = host.CreateContext();
        var account = await context.UserAccounts.AsNoTracking().SingleAsync();
        var director = await context.Directors.AsNoTracking().SingleAsync();
        var operation = await context.DirectorProvisioningOperations
            .AsNoTracking()
            .SingleAsync();
        var audit = await context.AuditLogs.AsNoTracking().SingleAsync();

        Assert.Equal(created.DirectorId, director.Id.Value);
        Assert.Equal(account.Id.Value, director.UserId.Value);
        Assert.Equal(created.OperationId, operation.Id.Value);
        Assert.Equal(created.DirectorId, operation.DirectorId.Value);
        Assert.NotEqual(
            EcoBillingEndToEndHost.InitialCredential,
            account.PasswordHash);
        Assert.Equal(created.DirectorId.ToString("D"), audit.EntityId);
        Assert.Equal("e2e-control-trace-1", audit.CorrelationId);
        Assert.DoesNotContain(
            EcoBillingEndToEndHost.InitialCredential,
            audit.AfterData ?? string.Empty,
            StringComparison.Ordinal);
    }

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(
            expectedCode,
            document.RootElement.GetProperty("code").GetString());
        Assert.False(
            string.IsNullOrWhiteSpace(
                document.RootElement.GetProperty("traceId").GetString()));
    }
}
