namespace EcoBilling.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    private const int MaximumCorrelationIdLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var suppliedCorrelationId = context.Request.Headers[HeaderName].ToString();
        if (IsValid(suppliedCorrelationId))
        {
            context.TraceIdentifier = suppliedCorrelationId;
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = context.TraceIdentifier;
            return Task.CompletedTask;
        });

        await next(context);
    }

    private static bool IsValid(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= MaximumCorrelationIdLength &&
        value.All(character => !char.IsControl(character));
}
