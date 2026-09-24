namespace EcoBilling.ArchitectureTests.ProjectDependencies;

public sealed class ProjectDependencyGraphValidatorTests
{
    [Fact]
    public void FindDisallowedDependencies_ReportsForbiddenEdge()
    {
        var graph = new ProjectDependencyGraph(["Modules", "Infrastructure"]);
        graph.AddDependency("Modules", "Infrastructure");
        var validatedProjects = new HashSet<string>(["Modules"]);
        var allowedDependencies = new HashSet<ProjectDependency>();

        var violations = ProjectDependencyGraphValidator.FindDisallowedDependencies(
            graph,
            validatedProjects,
            allowedDependencies);

        var violation = Assert.Single(violations);
        Assert.Equal("Modules", violation.Project);
        Assert.Equal("Infrastructure", violation.Dependency);
    }

    [Fact]
    public void FindDisallowedDependencies_AcceptsAllowedEdge()
    {
        var graph = new ProjectDependencyGraph(["Modules", "SharedKernel"]);
        graph.AddDependency("Modules", "SharedKernel");
        var validatedProjects = new HashSet<string>(["Modules"]);
        var allowedDependencies = new HashSet<ProjectDependency>
        {
            new("Modules", "SharedKernel")
        };

        var violations = ProjectDependencyGraphValidator.FindDisallowedDependencies(
            graph,
            validatedProjects,
            allowedDependencies);

        Assert.Empty(violations);
    }

    [Fact]
    public void FindCycle_ReportsCycleWithClosingProject()
    {
        var graph = new ProjectDependencyGraph(["Api", "Modules", "SharedKernel"]);
        graph.AddDependency("Api", "Modules");
        graph.AddDependency("Modules", "SharedKernel");
        graph.AddDependency("SharedKernel", "Api");

        var cycle = ProjectDependencyGraphValidator.FindCycle(graph);

        Assert.Equal(["Api", "Modules", "SharedKernel", "Api"], cycle);
    }

    [Fact]
    public void FindCycle_AcceptsAcyclicGraph()
    {
        var graph = new ProjectDependencyGraph(["Api", "Modules", "SharedKernel"]);
        graph.AddDependency("Api", "Modules");
        graph.AddDependency("Modules", "SharedKernel");

        var cycle = ProjectDependencyGraphValidator.FindCycle(graph);

        Assert.Empty(cycle);
    }
}
