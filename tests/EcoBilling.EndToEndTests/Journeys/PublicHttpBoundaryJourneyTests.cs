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

        Assert.All(
            host.RoutePatterns,
            route => Assert.True(
                route.StartsWith("/health", StringComparison.Ordinal) ||
                route.StartsWith("/internal/", StringComparison.Ordinal),
                $"Unexpected public route is exposed: {route}"));

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
