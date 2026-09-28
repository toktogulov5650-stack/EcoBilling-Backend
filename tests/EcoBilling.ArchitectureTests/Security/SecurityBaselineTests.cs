using System.Text.Json;
using EcoBilling.ArchitectureTests.ProjectDependencies;

namespace EcoBilling.ArchitectureTests.Security;

public sealed class SecurityBaselineTests
{
    [Fact]
    public void NuGetAudit_ExplicitlyIncludesTransitiveDependencies()
    {
        var buildProperties = ReadRepositoryFile("Directory.Build.props");

        Assert.Contains("<NuGetAudit>true</NuGetAudit>", buildProperties, StringComparison.Ordinal);
        Assert.Contains("<NuGetAuditMode>all</NuGetAuditMode>", buildProperties, StringComparison.Ordinal);
        Assert.Contains("<NuGetAuditLevel>low</NuGetAuditLevel>", buildProperties, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiAndCompose_DoNotUseWildcardAllowedHosts()
    {
        using var document = JsonDocument.Parse(
            ReadRepositoryFile("src", "EcoBilling.Api", "appsettings.json"));
        var allowedHosts = document.RootElement
            .GetProperty("AllowedHosts")
            .GetString();
        var compose = ReadRepositoryFile("deploy", "compose.yml");
        var environmentTemplate = ReadRepositoryFile("deploy", ".env.example");

        Assert.False(string.IsNullOrWhiteSpace(allowedHosts));
        Assert.DoesNotContain("*", allowedHosts, StringComparison.Ordinal);
        Assert.Contains("${ALLOWED_HOSTS:?", compose, StringComparison.Ordinal);
        Assert.Contains("ALLOWED_HOSTS=", environmentTemplate, StringComparison.Ordinal);
    }

    [Fact]
    public void UserJwtSigningKey_IsRequiredFromEnvironmentAndNotStoredInAppSettings()
    {
        var appSettings = ReadRepositoryFile("src", "EcoBilling.Api", "appsettings.json");
        var compose = ReadRepositoryFile("deploy", "compose.yml");
        var environmentTemplate = ReadRepositoryFile("deploy", ".env.example");

        Assert.DoesNotContain("SecretBase64", appSettings, StringComparison.Ordinal);
        Assert.Contains("${USER_AUTH_SIGNING_KEY:?", compose, StringComparison.Ordinal);
        Assert.Contains(
            "UserAuthentication__SigningKeys__0__SecretBase64",
            compose,
            StringComparison.Ordinal);
        Assert.Contains("USER_AUTH_SIGNING_KEY=", environmentTemplate, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(params string[] pathParts) =>
        File.ReadAllText(
            Path.Combine(
                [RepositoryPaths.RepositoryRoot, .. pathParts]));
}
