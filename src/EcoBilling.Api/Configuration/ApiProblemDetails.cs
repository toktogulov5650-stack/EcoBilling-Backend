using EcoBilling.SharedKernel.Errors;
using Microsoft.AspNetCore.Mvc;

namespace EcoBilling.Api.Configuration;

public static class ApiProblemDetails
{
    public static IResult Create(
        HttpContext httpContext,
        Error error,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
        var extensions = new Dictionary<string, object?>
        {
            ["code"] = error.Code,
            ["traceId"] = httpContext.TraceIdentifier
        };

        if (error.Type is ErrorType.Validation)
        {
            extensions["validationErrors"] = validationErrors ??
                new Dictionary<string, string[]>
                {
                    ["request"] = [error.Description]
                };
        }

        return Results.Problem(
            statusCode: statusCode,
            title: GetTitle(statusCode),
            detail: error.Description,
            instance: httpContext.Request.Path,
            extensions: extensions);
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Validation failed",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not found",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Internal server error"
    };
}
