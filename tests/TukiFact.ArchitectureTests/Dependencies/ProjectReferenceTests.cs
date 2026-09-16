using TukiFact.ArchitectureTests.Rules;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.Dependencies;

/// <summary>Project graph and package rules against the real csproj files.</summary>
public sealed class ProjectReferenceTests
{
    [Fact]
    public void References_FollowTheDependencyGraph()
    {
        var violations = ProjectReferenceRules.ReferencesFollowTheDependencyGraph(ProductionSolution.Projects);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Packages_DoNotReferenceForbiddenPackages()
    {
        var violations = ProjectReferenceRules.LayersDoNotReferenceForbiddenPackages(ProductionSolution.Projects);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Contracts_AssemblyReferencesOnlyTheBaseClassLibrary()
    {
        var violations = ProjectReferenceRules.AssemblyReferencesOnlyTheBaseClassLibrary(ProductionSolution.Contracts);

        violations.Should().BeEmpty();
    }
}
