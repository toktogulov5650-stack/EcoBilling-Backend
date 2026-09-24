namespace EcoBilling.ArchitectureTests.ProjectDependencies;

public sealed class SolutionProjectDependencyTests
{
    private static readonly HashSet<string> ProductionProjects =
    [
        "EcoBilling.Api",
        "EcoBilling.Infrastructure",
        "EcoBilling.Modules",
        "EcoBilling.SharedKernel",
        "EcoBilling.Worker"
    ];

    private static readonly HashSet<ProjectDependency> AllowedDependencies =
    [
        new("EcoBilling.Api", "EcoBilling.Infrastructure"),
        new("EcoBilling.Api", "EcoBilling.Modules"),
        new("EcoBilling.Worker", "EcoBilling.Infrastructure"),
        new("EcoBilling.Worker", "EcoBilling.Modules"),
        new("EcoBilling.Infrastructure", "EcoBilling.Modules"),
        new("EcoBilling.Infrastructure", "EcoBilling.SharedKernel"),
        new("EcoBilling.Modules", "EcoBilling.SharedKernel")
    ];

    [Fact]
    public void Solution_ContainsExpectedProductionProjects()
    {
        var graph = LoadSolutionGraph();

        Assert.Subset(graph.Projects.ToHashSet(StringComparer.Ordinal), ProductionProjects);
    }

    [Fact]
    public void ProductionProjects_UseOnlyAllowedDependencies()
    {
        var graph = LoadSolutionGraph();

        var violations = ProjectDependencyGraphValidator.FindDisallowedDependencies(
            graph,
            ProductionProjects,
            AllowedDependencies);

        Assert.Empty(violations);
    }

    [Fact]
    public void ProductionGraph_MatchesApprovedDependencies()
    {
        var graph = LoadSolutionGraph();
        var actualDependencies = ProductionProjects
            .SelectMany(project => graph.GetDependencies(project)
                .Select(dependency => new ProjectDependency(project, dependency)))
            .ToHashSet();

        Assert.True(
            AllowedDependencies.SetEquals(actualDependencies),
            "The production graph does not match the approved dependency set.");
    }

    [Fact]
    public void SolutionProjectGraph_HasNoCycles()
    {
        var graph = LoadSolutionGraph();

        var cycle = ProjectDependencyGraphValidator.FindCycle(graph);

        Assert.Empty(cycle);
    }

    private static ProjectDependencyGraph LoadSolutionGraph() =>
        ProjectDependencyGraphLoader.Load(RepositoryPaths.SolutionFile);
}
