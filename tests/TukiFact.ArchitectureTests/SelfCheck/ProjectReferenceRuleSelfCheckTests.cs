using TukiFact.ArchitectureTests.Rules;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.SelfCheck;

/// <summary>
/// Project graph and package rules report in-memory violating projects and accept the documented
/// graph. Unlike <see cref="DependencyRuleSelfCheckTests"/>, these
/// rules run on csproj data, so the fixtures are constructed in-memory records, not real projects.
/// </summary>
public sealed class ProjectReferenceRuleSelfCheckTests
{
    private const string Contracts = ProjectNaming.ContractsName;
    private const string GeneratedContracts = ProjectNaming.GeneratedContractsName;
    private const string Host = ProjectNaming.HostName;
    private const string CommonDomain = "TukiFact.Common.Domain";
    private const string CommonApplication = "TukiFact.Common.Application";
    private const string CommonInfrastructure = "TukiFact.Common.Infrastructure";
    private const string CommonPresentation = "TukiFact.Common.Presentation";
    private const string LedgerDomain = "TukiFact.Modules.Ledger.Domain";
    private const string LedgerApplication = "TukiFact.Modules.Ledger.Application";
    private const string LedgerInfrastructure = "TukiFact.Modules.Ledger.Infrastructure";
    private const string LedgerPresentation = "TukiFact.Modules.Ledger.Presentation";
    private const string FreightApplication = "TukiFact.Modules.Freight.Application";

    [Fact]
    public void References_ForbiddenEdgesAndUnknownProjects_AreReported()
    {
        // Arrange
        SolutionProject[] projects =
        [
            Project(LedgerApplication, projects: [FreightApplication]),
            Project(LedgerPresentation, projects: [LedgerInfrastructure]),
            Project(CommonPresentation, projects: [CommonInfrastructure, GeneratedContracts]),
            Project(Contracts, projects: [CommonDomain]),
            Project(GeneratedContracts, projects: [CommonDomain]),
            Project("TukiFact.Tools"),
        ];

        // Act
        var violations = ProjectReferenceRules.ReferencesFollowTheDependencyGraph(projects);

        // Assert
        violations.Should().ContainMatch($"{LedgerApplication} -> {FreightApplication}:*")
            .And.ContainMatch($"{LedgerPresentation} -> {LedgerInfrastructure}:*")
            .And.ContainMatch($"{CommonPresentation} -> {CommonInfrastructure}:*")
            .And.ContainMatch($"{CommonPresentation} -> {GeneratedContracts}:*")
            .And.ContainMatch($"{Contracts} -> {CommonDomain}:*")
            .And.ContainMatch($"{GeneratedContracts} -> {CommonDomain}:*")
            .And.ContainMatch("TukiFact.Tools:*");
    }

    [Fact]
    public void References_DocumentedGraph_AreNotReported()
    {
        // Arrange
        SolutionProject[] projects =
        [
            Project(Contracts),
            Project(GeneratedContracts),
            Project(CommonDomain),
            Project(CommonApplication, projects: [CommonDomain, Contracts]),
            Project(CommonInfrastructure, projects: [CommonApplication, Contracts]),
            Project(CommonPresentation, projects: [CommonApplication]),
            Project(LedgerDomain, projects: [CommonDomain]),
            Project(LedgerApplication, projects: [LedgerDomain, CommonApplication, Contracts]),
            Project(LedgerInfrastructure, projects: [LedgerApplication, CommonInfrastructure, Contracts]),
            Project(LedgerPresentation, projects: [LedgerApplication, CommonPresentation]),
            Project(Host, projects: [LedgerInfrastructure, LedgerPresentation, CommonInfrastructure, CommonPresentation]),
        ];

        // Act
        var violations = ProjectReferenceRules.ReferencesFollowTheDependencyGraph(projects);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void Packages_ForbiddenPackagesPerLayer_AreReported()
    {
        // Arrange
        SolutionProject[] projects =
        [
            Project(LedgerDomain, packages: ["Microsoft.EntityFrameworkCore"]),
            Project(LedgerApplication, packages: ["Npgsql", "FluentValidation"]),
            Project(LedgerInfrastructure, packages: ["MediatR"]),
            Project(LedgerPresentation, packages: ["Dapper"]),
            Project(Contracts, packages: ["Newtonsoft.Json"]),
            Project(GeneratedContracts, packages: ["Google.Protobuf"]),
        ];

        // Act
        var violations = ProjectReferenceRules.LayersDoNotReferenceForbiddenPackages(projects);

        // Assert
        violations.Should().ContainMatch($"{LedgerDomain}: package Microsoft.EntityFrameworkCore*")
            .And.ContainMatch($"{LedgerApplication}: package Npgsql*")
            .And.ContainMatch($"{LedgerInfrastructure}: package MediatR*")
            .And.ContainMatch($"{LedgerPresentation}: package Dapper*")
            .And.ContainMatch($"{Contracts}: package Newtonsoft.Json*")
            .And.NotContainMatch($"{LedgerApplication}: package FluentValidation*")
            .And.NotContainMatch($"{GeneratedContracts}:*");
    }

    [Fact]
    public void AssemblyReferences_AssemblyReferencingPackages_IsReported()
    {
        // Arrange
        var assembly = typeof(ProjectReferenceRuleSelfCheckTests).Assembly;

        // Act
        var violations = ProjectReferenceRules.AssemblyReferencesOnlyTheBaseClassLibrary(assembly);

        // Assert
        violations.Should().ContainMatch("NetArchTest.Rules*");
    }

    private static SolutionProject Project(
        string name,
        string[]? projects = null,
        string[]? packages = null,
        string[]? frameworks = null) =>
        new(name, string.Empty, projects ?? [], packages ?? [], frameworks ?? []);
}
