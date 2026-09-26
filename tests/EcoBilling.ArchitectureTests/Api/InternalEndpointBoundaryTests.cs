using EcoBilling.ArchitectureTests.ProjectDependencies;

namespace EcoBilling.ArchitectureTests.Api;

public sealed class InternalEndpointBoundaryTests
{
    [Theory]
    [InlineData("EcoBillingDbContext")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("EcoBilling.Infrastructure.Persistence")]
    public void InternalEndpoints_DoNotAccessPersistenceDirectly(
        string forbiddenText)
    {
        var sources = LoadInternalEndpointSources();

        Assert.NotEmpty(sources);
        Assert.All(
            sources,
            source => Assert.DoesNotContain(
                forbiddenText,
                source.Content,
                StringComparison.Ordinal));
    }

    [Fact]
    public void DirectorProvisioningEndpoint_RequiresDedicatedInternalPolicy()
    {
        var source = LoadInternalEndpointSources()
            .Single(file => file.Path.EndsWith(
                "DirectorProvisioningEndpoints.cs",
                StringComparison.Ordinal));

        Assert.Contains(
            ".RequireAuthorization(InternalServiceAuthenticationOptions.Policy)",
            source.Content,
            StringComparison.Ordinal);
    }

    private static SourceFile[] LoadInternalEndpointSources()
    {
        var solutionDirectory = Path.GetDirectoryName(RepositoryPaths.SolutionFile)
            ?? throw new InvalidOperationException("The solution directory is unavailable.");
        var directory = Path.Combine(
            solutionDirectory,
            "src",
            "EcoBilling.Api",
            "InternalEndpoints");

        return Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Select(path => new SourceFile(path, File.ReadAllText(path)))
            .ToArray();
    }

    private sealed record SourceFile(string Path, string Content);
}
