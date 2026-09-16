using TukiFact.ArchitectureTests.Rules;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.Conventions;

public sealed class MessagingConventionTests
{
    [Theory]
    [MemberData(nameof(ProductionSolution.LayerAssemblies), MemberType = typeof(ProductionSolution))]
    public void IntegrationEvents_LiveOnlyInContracts(string assembly)
    {
        var violations = MessagingConventions.IntegrationEventsLiveOnlyInContracts(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void IntegrationEventNames_FollowThePattern()
    {
        var contractsScope = new LayerScope(
            ProductionSolution.Contracts, ProjectNaming.ContractsName, Layer.Domain, Module: null, ProductionSolution.ModulesNamespaceRoot, []);

        var violations = MessagingConventions.IntegrationEventNamesFollowThePattern(contractsScope);

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Domain, MemberType = typeof(ProductionSolution))]
    public void DomainEventNames_FollowThePattern(string assembly)
    {
        var violations = MessagingConventions.DomainEventNamesFollowThePattern(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }
}
