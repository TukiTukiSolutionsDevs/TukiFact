using TukiFact.ArchitectureTests.Rules;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.Conventions;

public sealed class StructureConventionTests
{
    [Theory]
    [MemberData(nameof(ProductionSolution.LayerAssemblies), MemberType = typeof(ProductionSolution))]
    public void Layer_DoesNotUseHorizontalFolders(string assembly)
    {
        var violations = StructureConventions.DoesNotUseHorizontalFolders(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }
}
