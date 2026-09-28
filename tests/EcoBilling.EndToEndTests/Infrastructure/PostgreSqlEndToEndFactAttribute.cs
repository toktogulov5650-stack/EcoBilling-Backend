namespace EcoBilling.EndToEndTests.Infrastructure;

internal sealed class PostgreSqlEndToEndFactAttribute : FactAttribute
{
    internal const string ConnectionStringVariable =
        "ECOBILLING_TEST_POSTGRES_CONNECTION";

    public PostgreSqlEndToEndFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(ConnectionStringVariable)))
        {
            Skip = $"Set {ConnectionStringVariable} to run E2E tests against real PostgreSQL.";
        }
    }

    internal static string GetConnectionString() =>
        Environment.GetEnvironmentVariable(ConnectionStringVariable)
        ?? throw new InvalidOperationException(
            $"Environment variable {ConnectionStringVariable} is not configured.");
}
