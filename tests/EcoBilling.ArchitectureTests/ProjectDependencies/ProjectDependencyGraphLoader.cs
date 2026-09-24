using System.Xml.Linq;

namespace EcoBilling.ArchitectureTests.ProjectDependencies;

internal static class ProjectDependencyGraphLoader
{
    public static ProjectDependencyGraph Load(string solutionPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionPath);

        var fullSolutionPath = Path.GetFullPath(solutionPath);
        var solutionDirectory = Path.GetDirectoryName(fullSolutionPath)
            ?? throw new InvalidOperationException("The solution path has no parent directory.");
        var solution = XDocument.Load(fullSolutionPath);

        var projectPaths = solution
            .Descendants()
            .Where(element => element.Name.LocalName == "Project")
            .Select(element => element.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(Path.Combine(solutionDirectory, path!)))
            .ToArray();

        var projectsByPath = projectPaths.ToDictionary(
            path => path,
            path => Path.GetFileNameWithoutExtension(path),
            StringComparer.OrdinalIgnoreCase);
        var graph = new ProjectDependencyGraph(projectsByPath.Values);

        foreach (var (projectPath, projectName) in projectsByPath)
        {
            var project = XDocument.Load(projectPath);
            var projectDirectory = Path.GetDirectoryName(projectPath)
                ?? throw new InvalidOperationException($"Project '{projectPath}' has no parent directory.");

            var referencedPaths = project
                .Descendants()
                .Where(element => element.Name.LocalName == "ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.GetFullPath(Path.Combine(projectDirectory, path!)));

            foreach (var referencedPath in referencedPaths)
            {
                if (!projectsByPath.TryGetValue(referencedPath, out var dependencyName))
                {
                    throw new InvalidOperationException(
                        $"Project '{projectName}' references '{referencedPath}', which is not in the solution.");
                }

                graph.AddDependency(projectName, dependencyName);
            }
        }

        return graph;
    }
}
