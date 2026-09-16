using TukiFact.ArchitectureTests.Rules;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.Dependencies;

/// <summary>
/// Type-level dependency rules against every real Common/module assembly.
/// No module project exists yet, so the module-ban theory below is empty here — <c>SelfCheck</c>
/// (obs Fixtures/Modules/Ledger vs Freight) is what proves the rule mechanism is not vacuous.
/// </summary>
public sealed class LayerDependencyTests
{
    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Domain, MemberType = typeof(ProductionSolution))]
    public void Domain_DependsOnlyOnBaseClassLibraryAndDomain(string assembly)
    {
        var violations = DependencyRules.DomainDependsOnlyOnBaseClassLibraryAndDomain(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Application, MemberType = typeof(ProductionSolution))]
    public void Application_DoesNotDependOnInfrastructureOrPresentation(string assembly)
    {
        var violations = DependencyRules.ApplicationDoesNotDependOnInfrastructureOrPresentation(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Infrastructure, MemberType = typeof(ProductionSolution))]
    public void Infrastructure_DoesNotDependOnPresentation(string assembly)
    {
        var violations = DependencyRules.InfrastructureDoesNotDependOnPresentation(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Presentation, MemberType = typeof(ProductionSolution))]
    public void Presentation_DoesNotAccessData(string assembly)
    {
        var violations = DependencyRules.PresentationDoesNotAccessData(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Modules_DoNotDependOnOtherModules()
    {
        // No module project exists yet, so this is an empty-but-passing loop; a [Theory] with
        // no data rows fails outright under xUnit v3, unlike the other MemberData-backed rules here.
        var moduleAssemblies = ProductionSolution.Projects
            .Where(project => project.Kind == ProjectKind.Module)
            .Select(project => project.Name);

        var violations = moduleAssemblies
            .SelectMany(assembly => DependencyRules.ModuleDoesNotDependOnOtherModules(ProductionSolution.Scope(assembly)));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void CommonPresentation_DoesNotReferenceTheGeneratedGrpcContracts()
    {
        // The kernel stays product-agnostic: TukiFact.Contracts.Grpc is the
        // Invoicing service contract, and Common.Presentation reaches it only through the
        // IRpcErrorDetailFactory seam, never a direct reference.
        var project = ProductionSolution.Projects.Single(candidate => candidate.Name == "TukiFact.Common.Presentation");

        project.ProjectReferences.Should().NotContain(ProjectNaming.GeneratedContractsName);
    }
}
