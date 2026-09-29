using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EcoBilling.EndToEndTests.Infrastructure;

namespace EcoBilling.EndToEndTests.Journeys;

public sealed class PublicHttpBoundaryJourneyTests
{
    [PostgreSqlEndToEndFact]
    public async Task PublicRegistration_IsAbsentAndReturnsSanitizedNotFound()
    {
        await using var host = await EcoBillingEndToEndHost.CreateAsync();

        var expectedPublicRoutes = new[]
        {
            "/api/v1/accounts/by-number/{accountNumber}",
            "/api/v1/accounts/{accountId:guid}/billing/{year:int}/{month:int}",
            "/api/v1/accounts/{accountId:guid}/charges",
            "/api/v1/accounts/{accountId:guid}/financial",
            "/api/v1/accounts/{accountId:guid}/meters",
            "/api/v1/accounts/{accountId:guid}/payments",
            "/api/v1/accounts/{accountId:guid}/tariff",
            "/api/v1/accounts/{accountId:guid}/tariff-assignments",
            "/api/v1/accounts/{accountId:guid}/tariff-assignments/{assignmentId:guid}/end",
            "/api/v1/addresses/{addressId:guid}",
            "/api/v1/auth/login",
            "/api/v1/auth/refresh",
            "/api/v1/auth/revoke",
            "/api/v1/auth/setup-password",
            "/api/v1/charges/{chargeId:guid}",
            "/api/v1/controllers",
            "/api/v1/controllers/me/assignments",
            "/api/v1/controllers/me/profile",
            "/api/v1/controllers/me/worklist",
            "/api/v1/controllers/{controllerId:guid}",
            "/api/v1/controllers/{controllerId:guid}/assignments",
            "/api/v1/controllers/{controllerId:guid}/assignments/{assignmentId:guid}",
            "/api/v1/directors/me/profile",
            "/api/v1/me/account",
            "/api/v1/me/charges",
            "/api/v1/me/financial",
            "/api/v1/me/meters",
            "/api/v1/me/payments",
            "/api/v1/me/profile",
            "/api/v1/me/readings",
            "/api/v1/meters/{meterId:guid}",
            "/api/v1/meters/{meterId:guid}/readings",
            "/api/v1/meters/{meterId:guid}/replace",
            "/api/v1/payments/{paymentId:guid}",
            "/api/v1/readings/{readingId:guid}",
            "/api/v1/reports/financial-summary",
            "/api/v1/reports/operational-summary",
            "/api/v1/residents",
            "/api/v1/residents/{residentId:guid}",
            "/api/v1/residents/{residentId:guid}/password",
            "/api/v1/tariffs",
            "/api/v1/tariffs/{tariffId:guid}",
            "/api/v1/tariffs/{tariffId:guid}/versions",
            "/api/v1/tariffs/{tariffId:guid}/versions/{versionId:guid}",
            "/api/v1/tariffs/{tariffId:guid}/versions/{versionId:guid}/end",
            "/api/v1/users/{userId:guid}/sessions/revoke-all"
        };
        var actualPublicRoutes = host.RoutePatterns
            .Where(route =>
                !route.StartsWith("/health", StringComparison.Ordinal) &&
                !route.StartsWith("/internal/", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            expectedPublicRoutes.Order(StringComparer.Ordinal),
            actualPublicRoutes);
        Assert.DoesNotContain(
            actualPublicRoutes,
            route => route.Contains("register", StringComparison.OrdinalIgnoreCase));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/auth/register")
        {
            Content = JsonContent.Create(
                new
                {
                    Login = "self-registered@example.com",
                    Password = "must-not-be-accepted"
                })
        };
        request.Headers.Add("X-Correlation-Id", "e2e-public-trace-1");

        using var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "e2e-public-trace-1",
            response.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "request.not_found",
            document.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            "e2e-public-trace-1",
            document.RootElement.GetProperty("traceId").GetString());
        Assert.DoesNotContain(
            "self-registered@example.com",
            await response.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
    }
}
