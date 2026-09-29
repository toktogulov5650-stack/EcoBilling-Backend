using System.Text.RegularExpressions;
using EcoBilling.ArchitectureTests.ProjectDependencies;

namespace EcoBilling.ArchitectureTests.Documentation;

public sealed class DocumentationTests
{
    private static readonly Regex MarkdownLinkPattern = new(
        @"\[[^\]]+\]\((?<target>[^)\s]+)(?:\s+""[^""]*"")?\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void DocumentationIndex_ListsRequiredGuides()
    {
        var index = File.ReadAllText(
            Path.Combine(RepositoryPaths.RepositoryRoot, "docs", "README.md"));
        var requiredGuides = new[]
        {
            "project-status.md",
            "architecture/README.md",
            "business-rules/README.md",
            "api/README.md",
            "database/README.md",
            "configuration/README.md",
            "testing/README.md",
            "deployment/README.md",
            "operations/README.md",
            "operations/production-readiness.md",
            "security/README.md"
        };

        Assert.All(
            requiredGuides,
            guide => Assert.Contains($"({guide})", index, StringComparison.Ordinal));
    }

    [Fact]
    public void RootReadme_LinksToStatusConfigurationAndOperations()
    {
        var readme = File.ReadAllText(
            Path.Combine(RepositoryPaths.RepositoryRoot, "README.md"));

        Assert.Contains("(docs/project-status.md)", readme, StringComparison.Ordinal);
        Assert.Contains("(docs/configuration/README.md)", readme, StringComparison.Ordinal);
        Assert.Contains("(docs/operations/README.md)", readme, StringComparison.Ordinal);
        Assert.Contains("(docs/security/README.md)", readme, StringComparison.Ordinal);
        Assert.Contains("(docs/README.md)", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalMarkdownLinks_ResolveToRepositoryFiles()
    {
        var repositoryRoot = Path.GetFullPath(RepositoryPaths.RepositoryRoot);
        var repositoryPrefix = repositoryRoot + Path.DirectorySeparatorChar;
        var documents = Directory
            .EnumerateFiles(
                Path.Combine(repositoryRoot, "docs"),
                "*.md",
                SearchOption.AllDirectories)
            .Prepend(Path.Combine(repositoryRoot, "README.md"));

        foreach (var document in documents)
        {
            var content = File.ReadAllText(document);
            foreach (Match match in MarkdownLinkPattern.Matches(content))
            {
                var target = match.Groups["target"].Value;
                if (target.StartsWith('#') ||
                    Uri.TryCreate(target, UriKind.Absolute, out _))
                {
                    continue;
                }

                var pathWithoutFragment = target.Split('#', 2)[0];
                var relativePath = Uri.UnescapeDataString(pathWithoutFragment)
                    .Replace('/', Path.DirectorySeparatorChar);
                var documentDirectory = Path.GetDirectoryName(document)
                    ?? throw new DirectoryNotFoundException(
                        $"Could not find the directory for '{document}'.");
                var resolvedPath = Path.GetFullPath(
                    Path.Combine(documentDirectory, relativePath));

                Assert.True(
                    resolvedPath.StartsWith(
                        repositoryPrefix,
                        StringComparison.OrdinalIgnoreCase),
                    $"Documentation link escapes the repository: {document} -> {target}");
                Assert.True(
                    File.Exists(resolvedPath),
                    $"Documentation link does not resolve: {document} -> {target}");
            }
        }
    }
}
