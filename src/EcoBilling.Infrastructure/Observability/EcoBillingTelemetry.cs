namespace EcoBilling.Infrastructure.Observability;

public static class EcoBillingTelemetry
{
    public const string ApiServiceName = "EcoBilling.Api";
    public const string WorkerServiceName = "EcoBilling.Worker";
    public const string WorkerActivitySourceName = "EcoBilling.Worker";
    public const string WorkerMeterName = "EcoBilling.Worker";
    public const string OtlpEndpointConfigurationKey = "Observability:OtlpEndpoint";
}
