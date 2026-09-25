namespace EcoBilling.IntegrationTests.Infrastructure;

internal sealed class PostgreSqlFactAttribute : FactAttribute
{
    internal const string ConnectionStringVariable = "ECOBILLING_TEST_POSTGRES_CONNECTION";

    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringVariable)))
        {
            Skip = $"Set {ConnectionStringVariable} to run tests against real PostgreSQL.";
        }
    }

    internal static string GetConnectionString() =>
        Environment.GetEnvironmentVariable(ConnectionStringVariable)
        ?? throw new InvalidOperationException(
            $"Environment variable {ConnectionStringVariable} is not configured.");
}
