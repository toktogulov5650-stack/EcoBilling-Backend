namespace EcoBilling.ArchitectureTests.ProjectDependencies;

internal static class RepositoryPaths
{
    public static string SolutionFile
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "EcoBilling.slnx");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException(
                "Could not find EcoBilling.slnx from the test output directory.");
        }
    }
}
