using System.Reflection;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.Rules;

/// <summary>Project graph and package rules, checked on the csproj files.</summary>
public static class ProjectReferenceRules
{
    private static readonly string[] InfrastructurePackages = ["Microsoft.EntityFrameworkCore", "EFCore", "Npgsql"];

    /// <summary>
    /// Presentation/Infrastructure → Application → Domain of the same module, each on Common of the
    /// same layer; Contracts and the generated gRPC contracts reference nothing; a legacy project's
    /// own internal graph is out of scope, except the host, which may additionally reference kernel
    /// Infrastructure/Presentation.
    /// </summary>
    public static IReadOnlyList<string> ReferencesFollowTheDependencyGraph(IEnumerable<SolutionProject> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        SolutionProject[] all = [.. projects];

        var unknown = all
            .Where(project => project.Kind == ProjectKind.Unknown)
            .Select(project => $"{project.Name}: not a TukiFact.Common.<Layer>, TukiFact.Modules.<Module>.<Layer>, Contracts, generated-contracts or legacy project");

        var edges =
            from project in all
            where project.Kind != ProjectKind.Unknown
            from reference in project.ProjectReferences
            let reason = ForbiddenReason(project, SolutionProject.Named(reference))
            where reason is not null
            select $"{project.Name} -> {reference}: {reason}";

        return RuleResults.Sorted(unknown.Concat(edges));
    }

    /// <summary>Domain and Contracts take no packages; data packages stay out of Application/Presentation; no MediatR anywhere.</summary>
    public static IReadOnlyList<string> LayersDoNotReferenceForbiddenPackages(IEnumerable<SolutionProject> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        SolutionProject[] all = [.. projects];

        var packages =
            from project in all
            from package in project.PackageReferences
            let reason = ForbiddenPackageReason(project, package)
            where reason is not null
            select $"{project.Name}: package {package} ({reason})";

        return RuleResults.Sorted(packages);
    }

    /// <summary>Assembly references beyond the BCL (the compiled view of "references nothing").</summary>
    public static IReadOnlyList<string> AssemblyReferencesOnlyTheBaseClassLibrary(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return RuleResults.Sorted(assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => !(name.StartsWith("System.", StringComparison.Ordinal) || name is "System" or "netstandard" or "mscorlib")));
    }

    private static string? ForbiddenReason(SolutionProject from, SolutionProject to) => from switch
    {
        { Kind: ProjectKind.Contracts } => "Contracts references nothing",
        { Kind: ProjectKind.GeneratedContracts } => "the generated gRPC contracts reference nothing (BCL + packages only)",
        { Kind: ProjectKind.Legacy, Name: ProjectNaming.HostName } =>
            to is { Kind: ProjectKind.Common or ProjectKind.Module, Layer: Layer.Infrastructure or Layer.Presentation } or { Kind: ProjectKind.Legacy }
                ? null
                : "the host references only its legacy siblings, and kernel Infrastructure/Presentation projects",
        { Kind: ProjectKind.Legacy } => null, // the other 3 legacy projects keep their own pre-existing graph; out of scope here.
        { Kind: ProjectKind.Common, Layer: { } layer } => IsAllowedForCommon(layer, to)
            ? null
            : $"Common.{layer} may reference only {AllowedForCommon(layer)}",
        { Kind: ProjectKind.Module } when to.Kind == ProjectKind.Module && to.Module != from.Module =>
            "a module never references another module; share BuildingBlocks.Contracts integration events",
        { Kind: ProjectKind.Module, Layer: { } layer } => IsAllowedForModule(from, to)
            ? null
            : $"{layer} may reference only {AllowedForModule(layer)}",
        _ => null,
    };

    private static bool IsAllowedForCommon(Layer layer, SolutionProject to) => layer switch
    {
        Layer.Application => IsCommon(to, Layer.Domain) || to.Kind == ProjectKind.Contracts,
        Layer.Infrastructure => IsCommon(to, Layer.Application) || to.Kind == ProjectKind.Contracts,
        Layer.Presentation => IsCommon(to, Layer.Application),
        _ => false,
    };

    private static string AllowedForCommon(Layer layer) => layer switch
    {
        Layer.Application => "Common.Domain and Contracts",
        Layer.Infrastructure => "Common.Application and Contracts",
        Layer.Presentation => "Common.Application",
        _ => "nothing",
    };

    private static bool IsAllowedForModule(SolutionProject from, SolutionProject to) => from.Layer switch
    {
        Layer.Domain => IsCommon(to, Layer.Domain),
        Layer.Application => IsSameModule(from, to, Layer.Domain) || IsCommon(to, Layer.Application) || to.Kind == ProjectKind.Contracts,
        Layer.Infrastructure => IsSameModule(from, to, Layer.Application) || IsCommon(to, Layer.Infrastructure) || to.Kind == ProjectKind.Contracts,
        Layer.Presentation => IsSameModule(from, to, Layer.Application) || IsCommon(to, Layer.Presentation),
        _ => false,
    };

    private static string AllowedForModule(Layer layer) => layer switch
    {
        Layer.Domain => "Common.Domain",
        Layer.Application => "its Domain, Common.Application and Contracts",
        Layer.Infrastructure => "its Application, Common.Infrastructure and Contracts",
        _ => "its Application and Common.Presentation",
    };

    private static bool IsCommon(SolutionProject project, Layer layer) => project.Kind == ProjectKind.Common && project.Layer == layer;

    private static bool IsSameModule(SolutionProject from, SolutionProject to, Layer layer) =>
        to.Kind == ProjectKind.Module && to.Module == from.Module && to.Layer == layer;

    private static string? ForbiddenPackageReason(SolutionProject project, string package)
    {
        if (TakesNoPackages(project))
        {
            return $"{Describe(project)} takes no packages";
        }

        string[] forbidden = project.Layer switch
        {
            Layer.Application => [.. InfrastructurePackages, "Microsoft.AspNetCore", DependencyRules.MediatR],
            Layer.Presentation => [.. InfrastructurePackages, "Dapper", DependencyRules.MediatR],
            _ => [DependencyRules.MediatR],
        };

        var match = Array.Find(forbidden, prefix =>
            package == prefix || package.StartsWith(prefix + ".", StringComparison.Ordinal));

        return match is null ? null : $"{match} is not allowed in {Describe(project)}";
    }

    /// <summary>
    /// Contracts is BCL-only by design; the generated gRPC contracts are explicitly exempt —
    /// Grpc.Tools/Google.Protobuf are how the project exists at all.
    /// </summary>
    private static bool TakesNoPackages(SolutionProject project) =>
        project.Kind == ProjectKind.Contracts || project.Layer == Layer.Domain;

    private static string Describe(SolutionProject project) => project.Kind switch
    {
        ProjectKind.Contracts => "Contracts",
        ProjectKind.Common => $"Common.{project.Layer}",
        ProjectKind.Module => $"{project.Layer}",
        _ => project.Name,
    };
}
