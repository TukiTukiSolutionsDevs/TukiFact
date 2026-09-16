using TukiFact.ArchitectureTests.Rules;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.Conventions;

public sealed class ApplicationConventionTests
{
    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Application, MemberType = typeof(ProductionSolution))]
    public void Requests_ArePublicSealed(string assembly)
    {
        var violations = ApplicationConventions.RequestsArePublicSealed(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Application, MemberType = typeof(ProductionSolution))]
    public void HandlersAndValidators_AreInternalSealed(string assembly)
    {
        var violations = ApplicationConventions.HandlersAndValidatorsAreInternalSealed(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Application, MemberType = typeof(ProductionSolution))]
    public void HandlersAndValidators_AreNextToTheirRequest(string assembly)
    {
        var violations = ApplicationConventions.HandlersAndValidatorsAreNextToTheirRequest(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Application, MemberType = typeof(ProductionSolution))]
    public void QueryHandlers_DoNotUseTransactionManagerOrRepositoryPorts(string assembly)
    {
        var scope = ProductionSolution.Scope(assembly);
        var portScopes = ProductionSolution.ScopesOf(Layer.Domain).Concat(ProductionSolution.ScopesOf(Layer.Infrastructure));

        var violations = ApplicationConventions.QueryHandlersDoNotUseTransactionManagerOrRepositoryPorts(scope, portScopes);

        violations.Should().BeEmpty();
    }
}
