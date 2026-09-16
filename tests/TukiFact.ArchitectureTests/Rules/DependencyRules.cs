using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.Rules;

/// <summary>Type-level dependency rules.</summary>
public static class DependencyRules
{
    /// <summary>Frameworks configured and consumed only in Infrastructure (EF Core, Npgsql).</summary>
    internal static readonly string[] InfrastructureFrameworks = ["Microsoft.EntityFrameworkCore", "Npgsql"];

    /// <summary>The in-process facade is TukiFact's own mediator; MediatR is banned repo-wide (refactor-decisions).</summary>
    internal const string MediatR = "MediatR";

    private const string CoverletTracker = "Coverlet.Core.Instrumentation.Tracker";

    private static readonly string CommonDomain = $"{ProductionSolution.CommonNamespaceRoot}.{Layer.Domain}";
    private static readonly string CommonInfrastructure = $"{ProductionSolution.CommonNamespaceRoot}.{Layer.Infrastructure}";
    private static readonly string CommonPresentation = $"{ProductionSolution.CommonNamespaceRoot}.{Layer.Presentation}";

    /// <summary>Domain uses the BCL, Common.Domain and its own namespace only.</summary>
    public static IReadOnlyList<string> DomainDependsOnlyOnBaseClassLibraryAndDomain(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        // Microsoft.CodeAnalysis: compiler-embedded attributes (e.g. EmbeddedAttribute), not a package.
        // Coverlet.Core.Instrumentation.Tracker: hit counters coverlet injects on disk under `dotnet test --coverlet`.
        string[] allowed = ["System", "Microsoft.CodeAnalysis", CoverletTracker, CommonDomain, scope.NamespaceRoot];

        return RuleResults.FailingTypes(scope.Select().ShouldNot().HaveDependenciesOtherThan(allowed));
    }

    public static IReadOnlyList<string> ApplicationDoesNotDependOnInfrastructureOrPresentation(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        string[] forbidden =
        [
            .. InfrastructureFrameworks,
            "Microsoft.AspNetCore",
            MediatR,
            CommonInfrastructure,
            CommonPresentation,
            scope.SiblingNamespace(Layer.Infrastructure),
            scope.SiblingNamespace(Layer.Presentation),
        ];

        return RuleResults.FailingTypes(scope.Select().ShouldNot().HaveDependencyOnAny(forbidden));
    }

    /// <summary>Infrastructure cannot see endpoints: the host registers the Presentation assembly next to the module.</summary>
    public static IReadOnlyList<string> InfrastructureDoesNotDependOnPresentation(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        string[] forbidden = [CommonPresentation, scope.SiblingNamespace(Layer.Presentation)];

        return RuleResults.FailingTypes(scope.Select().ShouldNot().HaveDependencyOnAny(forbidden));
    }

    /// <summary>Presentation maps requests to the mediator; it never touches data frameworks or Infrastructure directly.</summary>
    public static IReadOnlyList<string> PresentationDoesNotAccessData(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        string[] forbidden =
        [
            .. InfrastructureFrameworks,
            "Dapper",
            "System.Data",
            MediatR,
            CommonInfrastructure,
            scope.SiblingNamespace(Layer.Infrastructure),
        ];

        return RuleResults.FailingTypes(scope.Select().ShouldNot().HaveDependencyOnAny(forbidden));
    }

    /// <summary>A module never references another module; they share integration events through BuildingBlocks.Contracts.</summary>
    public static IReadOnlyList<string> ModuleDoesNotDependOnOtherModules(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return scope.OtherModuleNamespaces.Count == 0
            ? []
            : RuleResults.FailingTypes(scope.Select().ShouldNot().HaveDependencyOnAny([.. scope.OtherModuleNamespaces]));
    }
}
