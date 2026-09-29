using EcoBilling.ArchitectureTests.ProjectDependencies;

namespace EcoBilling.ArchitectureTests.Deployment;

public sealed class ContinuousIntegrationTests
{
    [Fact]
    public void CiWorkflow_RunsCompleteReleaseVerificationWithPostgreSql()
    {
        var workflow = ReadWorkflow("ci.yml");

        AssertCommonVerification(workflow);
        Assert.Contains("docker compose", workflow, StringComparison.Ordinal);
        Assert.Contains("config --quiet", workflow, StringComparison.Ordinal);
        Assert.Contains("build api worker migrations", workflow, StringComparison.Ordinal);
        Assert.Contains(
            "aquasecurity/trivy-action@ed142fd0673e97e23eac54620cfb913e5ce36c25 # v0.36.0",
            workflow,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "uses: aquasecurity/trivy-action@v",
            workflow,
            StringComparison.Ordinal);
        Assert.Contains("severity: CRITICAL,HIGH", workflow, StringComparison.Ordinal);
        Assert.Contains("exit-code: \"1\"", workflow, StringComparison.Ordinal);
        Assert.Contains("image-ref: postgres:17-alpine", workflow, StringComparison.Ordinal);
        Assert.Contains("cancel-in-progress: true", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseWorkflow_VerifiesBeforePublishingArtifacts()
    {
        var workflow = ReadWorkflow("release.yml");

        AssertCommonVerification(workflow);
        Assert.Contains("dotnet publish src/EcoBilling.Api", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet publish src/EcoBilling.Worker", workflow, StringComparison.Ordinal);
        Assert.Contains("build api worker migrations", workflow, StringComparison.Ordinal);
        Assert.Contains(
            "aquasecurity/trivy-action@ed142fd0673e97e23eac54620cfb913e5ce36c25 # v0.36.0",
            workflow,
            StringComparison.Ordinal);
        Assert.Contains("image-ref: postgres:17-alpine", workflow, StringComparison.Ordinal);
        Assert.Contains(
            "actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7",
            workflow,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "uses: actions/upload-artifact@v",
            workflow,
            StringComparison.Ordinal);
        Assert.Contains("if-no-files-found: error", workflow, StringComparison.Ordinal);
    }

    private static void AssertCommonVerification(string workflow)
    {
        Assert.Contains("contents: read", workflow, StringComparison.Ordinal);
        Assert.Contains(
            "actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7",
            workflow,
            StringComparison.Ordinal);
        Assert.Contains(
            "actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6",
            workflow,
            StringComparison.Ordinal);
        Assert.DoesNotContain("uses: actions/checkout@v", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("uses: actions/setup-dotnet@v", workflow, StringComparison.Ordinal);
        Assert.Contains("persist-credentials: false", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet-version: 10.0.401", workflow, StringComparison.Ordinal);
        Assert.Contains("image: postgres:17-alpine", workflow, StringComparison.Ordinal);
        Assert.Contains("ECOBILLING_TEST_POSTGRES_CONNECTION", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet restore EcoBilling.slnx -warnaserror", workflow, StringComparison.Ordinal);
        Assert.Contains(
            "dotnet build EcoBilling.slnx --configuration Release --no-restore",
            workflow,
            StringComparison.Ordinal);
        Assert.Contains(
            "dotnet test EcoBilling.slnx --configuration Release --no-build",
            workflow,
            StringComparison.Ordinal);
        Assert.DoesNotContain("continue-on-error", workflow, StringComparison.Ordinal);
    }

    private static string ReadWorkflow(string fileName) =>
        File.ReadAllText(
            Path.Combine(RepositoryPaths.RepositoryRoot, ".github", "workflows", fileName));
}
