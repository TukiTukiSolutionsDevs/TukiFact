using TukiFact.ArchitectureTests.Rules;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.SelfCheck;

/// <summary>Dependency rules report the Ledger fixture violations and leave clean types and the Freight fixture alone.</summary>
public sealed class DependencyRuleSelfCheckTests
{
    [Fact]
    public void DomainDependencies_DomainTypeReferencingContracts_IsReported()
    {
        var scope = ViolationFixtures.Ledger(Layer.Domain);

        var violations = DependencyRules.DomainDependsOnlyOnBaseClassLibraryAndDomain(scope);

        violations.Should().ContainMatch("*.LedgerEntryPostedIntegrationEvent")
            .And.NotContainMatch("*.LedgerAccount");
    }

    [Fact]
    public void DomainDependencies_DomainOfACleanModule_AreNotReported()
    {
        var scope = ViolationFixtures.Freight(Layer.Domain);

        var violations = DependencyRules.DomainDependsOnlyOnBaseClassLibraryAndDomain(scope);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void ApplicationDependencies_HandlerUsingADbContext_IsReported()
    {
        var scope = ViolationFixtures.Ledger(Layer.Application);

        var violations = DependencyRules.ApplicationDoesNotDependOnInfrastructureOrPresentation(scope);

        violations.Should().ContainMatch("*.PostLedgerEntryCommandHandler")
            .And.NotContainMatch("*.GetLedgerAccountQueryHandler");
    }

    [Fact]
    public void InfrastructureDependencies_ModuleRegisteringAnEndpoint_IsReported()
    {
        var scope = ViolationFixtures.Ledger(Layer.Infrastructure);

        var violations = DependencyRules.InfrastructureDoesNotDependOnPresentation(scope);

        violations.Should().ContainMatch("*.LedgerModule")
            .And.NotContainMatch("*.LedgerAccountRepository");
    }

    [Fact]
    public void PresentationDependencies_EndpointUsingADbContext_IsReported()
    {
        var scope = ViolationFixtures.Ledger(Layer.Presentation);

        var violations = DependencyRules.PresentationDoesNotAccessData(scope);

        violations.Should().ContainMatch("*.PostLedgerEntryEndpoint")
            .And.NotContainMatch("*.GetLedgerAccountEndpoint");
    }

    [Fact]
    public void ModuleDependencies_HandlerUsingAnotherModule_IsReported()
    {
        var scope = ViolationFixtures.Ledger(Layer.Application);

        var violations = DependencyRules.ModuleDoesNotDependOnOtherModules(scope);

        violations.Should().ContainMatch("*.PostLedgerEntryCommandHandler")
            .And.NotContainMatch("*.GetLedgerAccountQueryHandler");
    }

    [Fact]
    public void ModuleDependencies_ModuleWithoutCrossReferences_AreNotReported()
    {
        var scope = ViolationFixtures.Freight(Layer.Application);

        var violations = DependencyRules.ModuleDoesNotDependOnOtherModules(scope);

        violations.Should().BeEmpty();
    }
}
