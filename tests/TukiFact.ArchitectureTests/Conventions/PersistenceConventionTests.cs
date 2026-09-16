using TukiFact.ArchitectureTests.Rules;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.Conventions;

public sealed class PersistenceConventionTests
{
    [Theory]
    [MemberData(nameof(ProductionSolution.LayerAssemblies), MemberType = typeof(ProductionSolution))]
    public void Repositories_HaveAtMostFivePublicMethods(string assembly)
    {
        var violations = PersistenceConventions.RepositoriesHaveAtMostFivePublicMethods(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.LayerAssemblies), MemberType = typeof(ProductionSolution))]
    public void Repositories_NeverExposeIQueryable(string assembly)
    {
        var violations = PersistenceConventions.RepositoriesNeverExposeIQueryable(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.LayerAssemblies), MemberType = typeof(ProductionSolution))]
    public void NoGenericRepositoryOrUnitOfWork(string assembly)
    {
        var violations = PersistenceConventions.NoGenericRepositoryOrUnitOfWork(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Infrastructure, MemberType = typeof(ProductionSolution))]
    public void OneDbContextPerModule(string assembly)
    {
        var violations = PersistenceConventions.OneDbContextPerModule(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.AssembliesOf), Layer.Domain, MemberType = typeof(ProductionSolution))]
    public void Entities_DoNotDeclareReservedShadowPropertyNames(string assembly)
    {
        var violations = PersistenceConventions.EntitiesDoNotDeclareReservedShadowPropertyNames(ProductionSolution.Scope(assembly));

        violations.Should().BeEmpty();
    }
}
