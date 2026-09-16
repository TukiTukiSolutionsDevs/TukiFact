namespace TukiFact.ArchitectureTests.Solution;

/// <summary>The rules scan what exists: every project follows the naming scheme, is built by the solution and is loaded.</summary>
public sealed class SolutionDiscoveryTests
{
    [Fact]
    public void SourceProjects_FollowTheNamingSchemeAndFolders()
    {
        // Arrange
        var projects = ProductionSolution.Projects;

        // Act
        var misplaced = projects
            .Where(project => project.ExpectedDirectory is null || project.RelativeDirectory != project.ExpectedDirectory)
            .Select(project => $"{project.Name}: at {project.RelativeDirectory}, expected {project.ExpectedDirectory ?? "a Common, module, Contracts, generated-contracts or legacy project name"}");

        // Assert
        misplaced.Should().BeEmpty("rules classify projects by name (TukiFact.Modules.<Module>.<Layer>, TukiFact.Common.<Layer>)");
    }

    [Fact]
    public void Projects_UnderSrcAndTests_AreInTheSolution()
    {
        // Arrange
        var solution = ProductionSolution.SolutionProjectNames();

        // Act
        var missing = ProductionSolution.Projects
            .Select(project => project.Name)
            .Concat(ProductionSolution.TestProjectNames())
            .Except(solution, StringComparer.Ordinal);

        // Assert
        missing.Should().BeEmpty("CI builds and tests the solution (TukiFact.slnx), so a project outside it is never checked");
    }

    [Fact]
    public void Modules_HaveTheFourLayerProjects()
    {
        // Arrange
        var modules = ProductionSolution.Modules;

        // Act
        var incomplete = modules.Where(module =>
            ProductionSolution.Projects.Count(project => project.Module == module) != Enum.GetValues<Layer>().Length);

        // Assert
        incomplete.Should().BeEmpty("a module is Domain, Application, Infrastructure and Presentation");
    }

    [Theory]
    [MemberData(nameof(ProductionSolution.LayerAssemblies), MemberType = typeof(ProductionSolution))]
    public void LayerAssembly_IsLoadedForScanning(string assembly)
    {
        // Act
        var scope = ProductionSolution.Scope(assembly);

        // Assert
        scope.Assembly.GetName().Name.Should().Be(assembly);
    }
}
