using EcoBilling.Modules.Identity.Domain;

namespace EcoBilling.ArchitectureTests.Identity;

public sealed class IdentityBoundaryTests
{
    [Theory]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Npgsql")]
    public void ModulesAssembly_DoesNotReferenceForbiddenFramework(string forbiddenPrefix)
    {
        var referencedAssemblies = typeof(UserAccount)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty);

        Assert.DoesNotContain(
            referencedAssemblies,
            assemblyName => assemblyName.StartsWith(forbiddenPrefix, StringComparison.Ordinal));
    }
}
