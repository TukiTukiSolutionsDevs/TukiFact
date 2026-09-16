using System.Reflection;
using System.Xml.Linq;
using TukiFact.ArchitectureTests.Rules;
using TukiFact.TestSupport;

namespace TukiFact.ArchitectureTests.Solution;

/// <summary>
/// Production code as the architecture tests see it: every csproj under <c>src/</c> (discovered,
/// never listed by hand) and its assembly, copied to the test output by the glob ProjectReference
/// of this project. A new module is covered by every rule as soon as its projects exist.
/// </summary>
public static class ProductionSolution
{
    public const string ModulesNamespaceRoot = "TukiFact.Modules";
    public const string CommonNamespaceRoot = "TukiFact.Common";

    private static readonly Lazy<IReadOnlyList<SolutionProject>> LazyProjects = new(() => LoadProjects("src"));

    public static string Root => SolutionRoot.Path;

    /// <summary>Every project under <c>src/</c>.</summary>
    public static IReadOnlyList<SolutionProject> Projects => LazyProjects.Value;

    public static IReadOnlyList<string> Modules =>
    [
        .. Projects
            .Where(project => project.Kind == ProjectKind.Module)
            .Select(project => project.Module!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal),
    ];

    public static Assembly Contracts => Load(ProjectNaming.ContractsName);

    /// <summary>Theory data: the Common and module assemblies of <paramref name="layer"/>.</summary>
    public static TheoryData<string> AssembliesOf(Layer layer) => Names(project => project.Layer == layer);

    /// <summary>Theory data: every Common and module assembly.</summary>
    public static TheoryData<string> LayerAssemblies() => Names(project => project.Layer is not null);

    /// <summary>Theory data: every Common assembly.</summary>
    public static TheoryData<string> CommonAssemblies() => Names(project => project.Kind == ProjectKind.Common);

    /// <summary>Theory data: every module assembly.</summary>
    public static TheoryData<string> ModuleAssemblies() => Names(project => project.Kind == ProjectKind.Module);

    public static IReadOnlyList<LayerScope> ScopesOf(Layer layer) =>
        [.. Projects.Where(project => project.Layer == layer).Select(project => Scope(project.Name))];

    public static LayerScope Scope(string assemblyName)
    {
        var project = Projects.Single(candidate => candidate.Name == assemblyName);
        var layer = project.Layer
            ?? throw new InvalidOperationException($"{assemblyName} is not a Common or module layer project.");

        return new LayerScope(
            Load(assemblyName),
            assemblyName,
            layer,
            project.Module,
            ModulesNamespaceRoot,
            [.. Modules.Where(module => module != project.Module)]);
    }

    /// <summary>Every project under <c>tests/</c>.</summary>
    public static IReadOnlyList<string> TestProjectNames() => [.. TestProjects().Select(project => project.Name)];

    /// <summary>Every project under <c>tests/</c>, with its references.</summary>
    public static IReadOnlyList<SolutionProject> TestProjects() => LoadProjects("tests");

    public static IReadOnlyList<string> SolutionProjectNames()
    {
        var slnx = Directory.EnumerateFiles(Root, "*.slnx").Single();

        return
        [
            .. XDocument.Load(slnx)
                .Descendants("Project")
                .Select(element => (string?)element.Attribute("Path"))
                .OfType<string>()
                .Select(path => Path.GetFileNameWithoutExtension(path.Replace('\\', '/')))
                .Order(StringComparer.Ordinal),
        ];
    }

    public static Assembly Load(string assemblyName) => Assembly.Load(new AssemblyName(assemblyName));

    private static TheoryData<string> Names(Func<SolutionProject, bool> predicate)
    {
        var data = new TheoryData<string>();
        foreach (var project in Projects.Where(predicate))
        {
            data.Add(project.Name);
        }

        return data;
    }

    private static List<SolutionProject> LoadProjects(string folder) =>
    [
        .. Directory.EnumerateFiles(Path.Combine(Root, folder), "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .Select(path => SolutionProject.Load(path, Root))
            .OrderBy(project => project.Name, StringComparer.Ordinal),
    ];

    private static bool IsBuildOutput(string path) =>
        Path.GetRelativePath(Root, path).Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj");
}
