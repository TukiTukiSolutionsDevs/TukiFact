using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.Dependencies;

/// <summary>
/// <see cref="ProjectNaming.LegacyProjects"/> is an exact set (ADR-011): the
/// pre-existing monolith never grows a fifth project, and every real project outside the kernel
/// naming scheme is one of these four, not silently reclassified as <see cref="ProjectKind.Unknown"/>.
/// </summary>
public sealed class LegacyAllowlistTests
{
    [Fact]
    public void LegacyProjects_MatchExactlyTheProjectsClassifiedAsLegacy()
    {
        var actual = ProductionSolution.Projects
            .Where(project => project.Kind == ProjectKind.Legacy)
            .Select(project => project.Name)
            .Order(StringComparer.Ordinal);

        actual.Should().BeEquivalentTo(ProjectNaming.LegacyProjects);
    }

    [Fact]
    public void LegacyProjects_IsExactlyTheDocumentedFour()
    {
        ProjectNaming.LegacyProjects.Should().BeEquivalentTo(
        [
            "TukiFact.Api",
            "TukiFact.Application",
            "TukiFact.Domain",
            "TukiFact.Infrastructure",
        ]);
    }
}
