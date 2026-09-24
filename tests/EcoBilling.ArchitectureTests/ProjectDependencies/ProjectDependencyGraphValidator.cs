namespace EcoBilling.ArchitectureTests.ProjectDependencies;

internal static class ProjectDependencyGraphValidator
{
    public static IReadOnlyCollection<ProjectDependencyViolation> FindDisallowedDependencies(
        ProjectDependencyGraph graph,
        IReadOnlySet<string> projectsToValidate,
        IReadOnlySet<ProjectDependency> allowedDependencies)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(projectsToValidate);
        ArgumentNullException.ThrowIfNull(allowedDependencies);

        var violations = new List<ProjectDependencyViolation>();

        foreach (var project in projectsToValidate.Order(StringComparer.Ordinal))
        {
            foreach (var dependency in graph.GetDependencies(project).Order(StringComparer.Ordinal))
            {
                var edge = new ProjectDependency(project, dependency);
                if (!allowedDependencies.Contains(edge))
                {
                    violations.Add(new ProjectDependencyViolation(project, dependency));
                }
            }
        }

        return violations;
    }

    public static IReadOnlyList<string> FindCycle(ProjectDependencyGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var states = graph.Projects.ToDictionary(
            project => project,
            _ => VisitState.NotVisited,
            StringComparer.Ordinal);
        var path = new List<string>();

        foreach (var project in graph.Projects.Order(StringComparer.Ordinal))
        {
            var cycle = Visit(project, graph, states, path);
            if (cycle.Count > 0)
            {
                return cycle;
            }
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> Visit(
        string project,
        ProjectDependencyGraph graph,
        IDictionary<string, VisitState> states,
        IList<string> path)
    {
        if (states[project] is VisitState.Visited)
        {
            return Array.Empty<string>();
        }

        if (states[project] is VisitState.Visiting)
        {
            var cycleStart = path.IndexOf(project);
            return path.Skip(cycleStart).Append(project).ToArray();
        }

        states[project] = VisitState.Visiting;
        path.Add(project);

        foreach (var dependency in graph.GetDependencies(project).Order(StringComparer.Ordinal))
        {
            var cycle = Visit(dependency, graph, states, path);
            if (cycle.Count > 0)
            {
                return cycle;
            }
        }

        path.RemoveAt(path.Count - 1);
        states[project] = VisitState.Visited;
        return Array.Empty<string>();
    }

    private enum VisitState
    {
        NotVisited,
        Visiting,
        Visited
    }
}

internal sealed record ProjectDependency(string Project, string Dependency);

internal sealed record ProjectDependencyViolation(string Project, string Dependency);
