using EcoBilling.ArchitectureTests.ProjectDependencies;

namespace EcoBilling.ArchitectureTests.Deployment;

public sealed class DockerDeploymentTests
{
    [Fact]
    public void RuntimeImages_UseNonRootUsersAndHealthChecks()
    {
        var apiDockerfile = ReadDeploymentFile("Dockerfile.api");
        var workerDockerfile = ReadDeploymentFile("Dockerfile.worker");

        Assert.Contains("sdk:10.0.401", apiDockerfile, StringComparison.Ordinal);
        Assert.Contains("aspnet:10.0.12", apiDockerfile, StringComparison.Ordinal);
        Assert.Contains("sdk:10.0.401", workerDockerfile, StringComparison.Ordinal);
        Assert.Contains("runtime:10.0.12", workerDockerfile, StringComparison.Ordinal);
        Assert.Contains("USER $APP_UID", apiDockerfile, StringComparison.Ordinal);
        Assert.Contains("USER $APP_UID", workerDockerfile, StringComparison.Ordinal);
        Assert.Contains(
            "NUGET_PACKAGES=/src/.nuget/packages",
            apiDockerfile,
            StringComparison.Ordinal);
        Assert.Contains("HEALTHCHECK", apiDockerfile, StringComparison.Ordinal);
        Assert.Contains("HEALTHCHECK", workerDockerfile, StringComparison.Ordinal);
        Assert.Contains("STOPSIGNAL SIGTERM", apiDockerfile, StringComparison.Ordinal);
        Assert.Contains("STOPSIGNAL SIGTERM", workerDockerfile, StringComparison.Ordinal);
        Assert.DoesNotContain("COPY . .", apiDockerfile, StringComparison.Ordinal);
        Assert.DoesNotContain("COPY . .", workerDockerfile, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_RequiresSecretsAndUsesApprovedConfigurationKeys()
    {
        var compose = ReadDeploymentFile("compose.yml");

        Assert.Contains("image: ecobilling-postgres:latest", compose, StringComparison.Ordinal);
        Assert.Contains("dockerfile: deploy/Dockerfile.postgres", compose, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings__EcoBilling", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectionStrings__Database", compose, StringComparison.Ordinal);
        Assert.Contains("${POSTGRES_PASSWORD:?", compose, StringComparison.Ordinal);
        Assert.Contains(
            "${DIRECTOR_PROVISIONING_FINGERPRINT_KEY:?",
            compose,
            StringComparison.Ordinal);
        Assert.Contains(
            "${INTERNAL_SERVICE_PUBLIC_KEY_PEM:?",
            compose,
            StringComparison.Ordinal);
        Assert.DoesNotContain("ecobilling_dev", compose, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_WaitsForDatabaseAndMigrationsBeforeApplications()
    {
        var compose = ReadDeploymentFile("compose.yml");

        Assert.Contains("migrations:", compose, StringComparison.Ordinal);
        Assert.Contains("condition: service_healthy", compose, StringComparison.Ordinal);
        Assert.Contains(
            "condition: service_completed_successfully",
            compose,
            StringComparison.Ordinal);
        Assert.Contains("/health/ready", compose, StringComparison.Ordinal);
        Assert.Contains("no-new-privileges:true", compose, StringComparison.Ordinal);
        Assert.Contains("read_only: true", compose, StringComparison.Ordinal);
    }

    [Fact]
    public void EnvironmentTemplate_ContainsNoPrivateKey()
    {
        var environmentTemplate = ReadDeploymentFile(".env.example");

        Assert.Contains("BEGIN PUBLIC KEY", environmentTemplate, StringComparison.Ordinal);
        Assert.DoesNotContain("BEGIN PRIVATE KEY", environmentTemplate, StringComparison.Ordinal);
        Assert.DoesNotContain("BEGIN RSA PRIVATE KEY", environmentTemplate, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionPostgresRoleScripts_SeparateMigratorAndRuntime()
    {
        var bootstrap = File.ReadAllText(
            Path.Combine(
                RepositoryPaths.RepositoryRoot,
                "deploy",
                "postgres",
                "bootstrap-production-roles.sql"));
        var grants = File.ReadAllText(
            Path.Combine(
                RepositoryPaths.RepositoryRoot,
                "deploy",
                "postgres",
                "grant-runtime.sql"));

        Assert.Contains("ecobilling_migrator", bootstrap, StringComparison.Ordinal);
        Assert.Contains("ecobilling_runtime", bootstrap, StringComparison.Ordinal);
        Assert.Contains("NOSUPERUSER", bootstrap, StringComparison.Ordinal);
        Assert.Contains("NOCREATEDB", bootstrap, StringComparison.Ordinal);
        Assert.Contains("REVOKE UPDATE, DELETE", grants, StringComparison.Ordinal);
        Assert.Contains("infrastructure.audit_logs", grants, StringComparison.Ordinal);
    }

    [Fact]
    public void DockerBuildContext_ExcludesLocalEnvironmentFiles()
    {
        var dockerIgnore = File.ReadAllText(
            Path.Combine(RepositoryPaths.RepositoryRoot, ".dockerignore"));

        Assert.Contains("deploy/.env", dockerIgnore, StringComparison.Ordinal);
        Assert.Contains("!deploy/.env.example", dockerIgnore, StringComparison.Ordinal);
    }

    private static string ReadDeploymentFile(string fileName) =>
        File.ReadAllText(
            Path.Combine(RepositoryPaths.RepositoryRoot, "deploy", fileName));
}
