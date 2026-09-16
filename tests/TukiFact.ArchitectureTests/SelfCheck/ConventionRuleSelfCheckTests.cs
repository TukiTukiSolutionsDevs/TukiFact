using TukiFact.ArchitectureTests.Rules;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.SelfCheck;

/// <summary>
/// Every convention rule reports the Ledger fixture violations and
/// leaves clean types and the Freight fixture alone.
/// </summary>
public sealed class ConventionRuleSelfCheckTests
{
    [Fact]
    public void Structure_HorizontalFolder_IsReported()
    {
        var violations = StructureConventions.DoesNotUseHorizontalFolders(ViolationFixtures.Ledger(Layer.Infrastructure));

        violations.Should().ContainMatch("*.LedgerEntryLegacyRepositoryStore")
            .And.NotContainMatch("*.LedgerAccountRepository");
    }

    [Fact]
    public void Structure_TopLevelSharedFolder_IsReported()
    {
        var violations = StructureConventions.DoesNotUseHorizontalFolders(ViolationFixtures.Ledger(Layer.Application));

        violations.Should().ContainMatch("*.LedgerAmount");
    }

    [Fact]
    public void Structure_NestedSharedFolderInACleanModule_IsNotReported()
    {
        var violations = StructureConventions.DoesNotUseHorizontalFolders(ViolationFixtures.Freight(Layer.Application));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Application_RequestNotPublicSealed_IsReported()
    {
        var violations = ApplicationConventions.RequestsArePublicSealed(ViolationFixtures.Ledger(Layer.Application));

        violations.Should().ContainMatch("*.ArchiveLedgerEntryCommand")
            .And.NotContainMatch("*.PostLedgerEntryCommand");
    }

    [Fact]
    public void Application_RequestsOfACleanModule_AreNotReported()
    {
        var violations = ApplicationConventions.RequestsArePublicSealed(ViolationFixtures.Freight(Layer.Application));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Application_HandlerNotInternalSealed_IsReported()
    {
        var violations = ApplicationConventions.HandlersAndValidatorsAreInternalSealed(ViolationFixtures.Ledger(Layer.Application));

        violations.Should().ContainMatch("*.PostLedgerEntryCommandHandler")
            .And.NotContainMatch("*.GetLedgerAccountQueryHandler");
    }

    [Fact]
    public void Application_HandlerNotNextToItsRequest_IsReported()
    {
        var violations = ApplicationConventions.HandlersAndValidatorsAreNextToTheirRequest(ViolationFixtures.Ledger(Layer.Application));

        violations.Should().ContainMatch("*.VoidLedgerEntryCommandHandler")
            .And.NotContainMatch("*.GetLedgerAccountQueryHandler");
    }

    [Fact]
    public void Application_HandlersOfACleanModule_AreNotReported()
    {
        var violations = ApplicationConventions.HandlersAndValidatorsAreInternalSealed(ViolationFixtures.Freight(Layer.Application))
            .Concat(ApplicationConventions.HandlersAndValidatorsAreNextToTheirRequest(ViolationFixtures.Freight(Layer.Application)))
            .ToArray();

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Application_QueryHandlerUsingARepositoryPort_IsReported()
    {
        var scope = ViolationFixtures.Ledger(Layer.Application);
        LayerScope[] portScopes = [ViolationFixtures.Ledger(Layer.Domain)];

        var violations = ApplicationConventions.QueryHandlersDoNotUseTransactionManagerOrRepositoryPorts(scope, portScopes);

        violations.Should().ContainMatch("*.GetLedgerEntryDetailQueryHandler")
            .And.NotContainMatch("*.GetLedgerAccountQueryHandler");
    }

    [Fact]
    public void Application_QueryHandlerOfACleanModule_IsNotReported()
    {
        var scope = ViolationFixtures.Freight(Layer.Application);
        LayerScope[] portScopes = [ViolationFixtures.Freight(Layer.Domain)];

        var violations = ApplicationConventions.QueryHandlersDoNotUseTransactionManagerOrRepositoryPorts(scope, portScopes);

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Persistence_RepositoryWithTooManyMethods_IsReported()
    {
        var violations = PersistenceConventions.RepositoriesHaveAtMostFivePublicMethods(ViolationFixtures.Ledger(Layer.Domain));

        violations.Should().ContainMatch("*.ILedgerAccountRepository");
    }

    [Fact]
    public void Persistence_RepositoryExposingIQueryable_IsReported()
    {
        var violations = PersistenceConventions.RepositoriesNeverExposeIQueryable(ViolationFixtures.Ledger(Layer.Domain));

        violations.Should().ContainMatch("*.ILedgerAccountRepository");
    }

    [Fact]
    public void Persistence_CleanRepository_IsNotReported()
    {
        var violations = PersistenceConventions.RepositoriesHaveAtMostFivePublicMethods(ViolationFixtures.Freight(Layer.Domain))
            .Concat(PersistenceConventions.RepositoriesNeverExposeIQueryable(ViolationFixtures.Freight(Layer.Domain)))
            .ToArray();

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Persistence_GenericRepositoryAndUnitOfWork_AreReported()
    {
        var violations = PersistenceConventions.NoGenericRepositoryOrUnitOfWork(ViolationFixtures.Ledger(Layer.Domain));

        violations.Should().ContainMatch("*.IRepository*")
            .And.ContainMatch("*.IUnitOfWork");
    }

    [Fact]
    public void Persistence_CleanModule_HasNoGenericRepositoryOrUnitOfWork()
    {
        var violations = PersistenceConventions.NoGenericRepositoryOrUnitOfWork(ViolationFixtures.Freight(Layer.Domain));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Persistence_WrongNameDbContext_IsReported()
    {
        var violations = PersistenceConventions.OneDbContextPerModule(ViolationFixtures.Ledger(Layer.Infrastructure));

        violations.Should().ContainMatch("*.WrongNameDbContext*");
    }

    [Fact]
    public void Persistence_CorrectlyNamedDbContext_IsNotReported()
    {
        var violations = PersistenceConventions.OneDbContextPerModule(ViolationFixtures.Freight(Layer.Infrastructure));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Persistence_EntityDeclaringAReservedShadowProperty_IsReported()
    {
        var violations = PersistenceConventions.EntitiesDoNotDeclareReservedShadowPropertyNames(ViolationFixtures.Ledger(Layer.Domain));

        violations.Should().ContainMatch("*.LedgerJournalEntry.CreatedAt")
            .And.NotContainMatch("*.LedgerAccount*");
    }

    [Fact]
    public void Persistence_CleanEntities_AreNotReported()
    {
        var violations = PersistenceConventions.EntitiesDoNotDeclareReservedShadowPropertyNames(ViolationFixtures.Freight(Layer.Domain));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Messaging_IntegrationEventOutsideContracts_IsReported()
    {
        var violations = MessagingConventions.IntegrationEventsLiveOnlyInContracts(ViolationFixtures.Ledger(Layer.Domain));

        violations.Should().ContainMatch("*.LedgerEntryPostedIntegrationEvent")
            .And.ContainMatch("*.LedgerBadEvent");
    }

    [Fact]
    public void Messaging_NoIntegrationEventInACleanModule_IsNotReported()
    {
        var violations = MessagingConventions.IntegrationEventsLiveOnlyInContracts(ViolationFixtures.Freight(Layer.Domain));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Messaging_BadlyNamedIntegrationEvent_IsReported()
    {
        var violations = MessagingConventions.IntegrationEventNamesFollowThePattern(ViolationFixtures.Ledger(Layer.Domain));

        violations.Should().ContainMatch("*.LedgerBadEvent")
            .And.NotContainMatch("*.LedgerEntryPostedIntegrationEvent");
    }

    [Fact]
    public void Messaging_BadlyNamedDomainEvent_IsReported()
    {
        var violations = MessagingConventions.DomainEventNamesFollowThePattern(ViolationFixtures.Ledger(Layer.Domain));

        violations.Should().ContainMatch("*.LedgerStuffHappened");
    }

    [Fact]
    public void Messaging_CorrectlyNamedDomainEventInACleanModule_IsNotReported()
    {
        var violations = MessagingConventions.DomainEventNamesFollowThePattern(ViolationFixtures.Freight(Layer.Domain));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Composition_MoreThanOneModule_IsReported()
    {
        var violations = CompositionConventions.ExactlyOneModuleAtTheExpectedName(ViolationFixtures.Ledger(Layer.Infrastructure));

        violations.Should().ContainMatch("*.LedgerModule*")
            .And.ContainMatch("*.LedgerLegacyModule*");
    }

    [Fact]
    public void Composition_ExactlyOneModule_IsNotReported()
    {
        var violations = CompositionConventions.ExactlyOneModuleAtTheExpectedName(ViolationFixtures.Freight(Layer.Infrastructure));

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Composition_EndpointNotUnderASubmodule_IsReported()
    {
        var violations = CompositionConventions.EndpointsAreSealedNamedByUseCaseAndUnderASubmodule(ViolationFixtures.Ledger(Layer.Presentation));

        violations.Should().ContainMatch("*.GetLedgerAccountEndpoint")
            .And.ContainMatch("*.PostLedgerEntryEndpoint");
    }

    [Fact]
    public void Composition_EndpointUnderASubmoduleInACleanModule_IsNotReported()
    {
        var violations = CompositionConventions.EndpointsAreSealedNamedByUseCaseAndUnderASubmodule(ViolationFixtures.Freight(Layer.Presentation));

        violations.Should().BeEmpty();
    }
}
