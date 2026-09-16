using System.Xml.Linq;

namespace TukiFact.ArchitectureTests.Solution;

/// <summary>A csproj of the repository: its role (from <see cref="ProjectNaming"/>) and the references it declares.</summary>
public sealed record SolutionProject(
    string Name,
    string RelativeDirectory,
    IReadOnlyCollection<string> ProjectReferences,
    IReadOnlyCollection<string> PackageReferences,
    IReadOnlyCollection<string> FrameworkReferences)
{
    private readonly (ProjectKind Kind, string? Module, Layer? Layer) _role = Classify(Name);

    public ProjectKind Kind => _role.Kind;

    public string? Module => _role.Module;

    public Layer? Layer => _role.Layer;

    /// <summary>Folder the naming scheme expects, relative to the repository root; <see langword="null"/> for unknown projects.</summary>
    public string? ExpectedDirectory => Kind switch
    {
        ProjectKind.Contracts => $"src/BuildingBlocks/{Name}",
        ProjectKind.GeneratedContracts => $"src/BuildingBlocks/{Name}",
        ProjectKind.Common => $"src/Common/{Name}",
        ProjectKind.Module => $"src/Modules/{Module}/{Name}",
        ProjectKind.Legacy => $"src/{Name}",
        _ => null,
    };

    /// <summary>A referenced project known only by name (its role is all a reference rule needs).</summary>
    public static SolutionProject Named(string name) => new(name, string.Empty, [], [], []);

    public static SolutionProject Load(string csprojPath, string repositoryRoot)
    {
        var document = XDocument.Load(csprojPath);
        var directory = Path.GetRelativePath(repositoryRoot, Path.GetDirectoryName(csprojPath)!).Replace('\\', '/');

        return new SolutionProject(
            Path.GetFileNameWithoutExtension(csprojPath),
            directory,
            [.. Includes(document, "ProjectReference").Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))],
            [.. Includes(document, "PackageReference")],
            [.. Includes(document, "FrameworkReference")]);
    }

    private static IEnumerable<string> Includes(XDocument document, string item) =>
        document.Descendants(item).Select(element => (string?)element.Attribute("Include")).OfType<string>();

    private static (ProjectKind Kind, string? Module, Layer? Layer) Classify(string name)
    {
        if (name == ProjectNaming.ContractsName)
        {
            return (ProjectKind.Contracts, null, null);
        }

        if (name == ProjectNaming.GeneratedContractsName)
        {
            return (ProjectKind.GeneratedContracts, null, null);
        }

        if (ProjectNaming.LegacyProjects.Contains(name, StringComparer.Ordinal))
        {
            return (ProjectKind.Legacy, null, null);
        }

        var parts = name.Split('.');
        if (name.StartsWith(ProjectNaming.CommonPrefix, StringComparison.Ordinal) && parts.Length == 3 && TryParseLayer(parts[2], out var commonLayer))
        {
            return (ProjectKind.Common, null, commonLayer);
        }

        if (name.StartsWith(ProjectNaming.ModulePrefix, StringComparison.Ordinal) && parts.Length == 4 && TryParseLayer(parts[3], out var moduleLayer))
        {
            return (ProjectKind.Module, parts[2], moduleLayer);
        }

        return (ProjectKind.Unknown, null, null);
    }

    private static bool TryParseLayer(string value, out Layer layer) =>
        Enum.TryParse(value, ignoreCase: false, out layer) && Enum.IsDefined(layer) && layer.ToString() == value;
}
