using TukiFact.ArchitectureTests.Rules;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.Conventions;

public sealed class CompositionConventionTests
{
    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Infrastructure, MemberType = typeof(ProductionSolution))]
    public void ExactlyOneModule_AtTheExpectedName(string assembly)
    {
        var violations = CompositionConventions.ExactlyOneModuleAtTheExpectedName(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Presentation, MemberType = typeof(ProductionSolution))]
    public void Endpoints_AreSealedNamedByUseCaseAndUnderASubmodule(string assembly)
    {
        var violations = CompositionConventions.EndpointsAreSealedNamedByUseCaseAndUnderASubmodule(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }
}
