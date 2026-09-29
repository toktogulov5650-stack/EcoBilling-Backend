using EcoBilling.Api.Configuration;
using EcoBilling.Modules.Meters.Domain;
using EcoBilling.Modules.Readings.Domain;
using EcoBilling.Modules.Readings.Features.Add;
using EcoBilling.Modules.Readings.Features.ListByMeter;

namespace EcoBilling.Api.Endpoints;

public static class ReadingEndpoints
{
    public static IEndpointRouteBuilder MapReadingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/meters/{meterId:guid}/readings", AddAsync)
            .RequireAuthorization(UserAuthenticationOptions.ControllerOrDirectorPolicy)
            .WithTags("Readings")
            .WithName("AddMeterReading");

        endpoints.MapGet("/api/v1/meters/{meterId:guid}/readings", ListAsync)
            .RequireAuthorization(UserAuthenticationOptions.DirectorPolicy)
            .WithTags("Readings")
            .WithName("ListMeterReadings");

        return endpoints;
    }

    private static async Task<IResult> AddAsync(
        Guid meterId,
        AddMeterReadingRequest request,
        HttpContext httpContext,
        AddMeterReadingHandler handler,
        CancellationToken cancellationToken)
    {
        if (meterId == Guid.Empty)
        {
            return ApiProblemDetails.Create(httpContext, MeterReadingErrors.MeterNotFound);
        }

        var role = UserRequestContext.GetRole(httpContext);
        var source = request.SupersedesReadingId is null
            ? ReadingSource.Manual
            : ReadingSource.Correction;

        var supersedes = request.SupersedesReadingId is { } id && id != Guid.Empty
            ? new MeterReadingId(id)
            : null;

        var result = await handler.Handle(
            new AddMeterReadingCommand(
                new MeterId(meterId),
                request.Value,
                request.MeasuredAt,
                UserRequestContext.GetUserId(httpContext),
                role,
                source,
                supersedes,
                request.Reason,
                httpContext.TraceIdentifier),
            cancellationToken);

        return result.IsFailure
            ? ApiProblemDetails.Create(httpContext, result.Error)
            : Results.Created(
                $"/api/v1/readings/{result.Value.Value:D}",
                new ReadingMutationResponse(result.Value.Value, "created"));
    }

    private static async Task<IResult> ListAsync(
        Guid meterId,
        HttpContext httpContext,
        ListReadingsByMeterHandler handler,
        CancellationToken cancellationToken)
    {
        if (meterId == Guid.Empty)
        {
            return ApiProblemDetails.Create(httpContext, MeterReadingErrors.MeterNotFound);
        }

        var result = await handler.Handle(
            new ListReadingsByMeterQuery(new MeterId(meterId)),
            cancellationToken);

        return Results.Ok(result.Value);
    }
}

public sealed record AddMeterReadingRequest(
    decimal Value,
    DateTimeOffset MeasuredAt,
    Guid? SupersedesReadingId,
    string? Reason);

public sealed record ReadingMutationResponse(Guid ReadingId, string Status);
