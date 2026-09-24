namespace EcoBilling.ArchitectureTests.ProjectDependencies;

internal sealed class ProjectDependencyGraph
{
    private readonly Dictionary<string, HashSet<string>> dependencies;

    public ProjectDependencyGraph(IEnumerable<string> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        dependencies = projects
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(
                project => project,
                _ => new HashSet<string>(StringComparer.Ordinal),
                StringComparer.Ordinal);
    }

    public IReadOnlyCollection<string> Projects => dependencies.Keys;

    public void AddDependency(string project, string dependency)
    {
        if (!dependencies.TryGetValue(project, out var projectDependencies))
        {
            throw new ArgumentException($"Unknown project '{project}'.", nameof(project));
        }

        if (!dependencies.ContainsKey(dependency))
        {
            throw new ArgumentException($"Unknown dependency '{dependency}'.", nameof(dependency));
        }

        projectDependencies.Add(dependency);
    }

    public IReadOnlyCollection<string> GetDependencies(string project)
    {
        if (!dependencies.TryGetValue(project, out var projectDependencies))
        {
            throw new ArgumentException($"Unknown project '{project}'.", nameof(project));
        }

        return projectDependencies;
    }
}
