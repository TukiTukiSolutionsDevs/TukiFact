using System.Reflection;
using System.Runtime.CompilerServices;
using NetArchTest.Rules;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.Rules;

/// <summary>
/// What one rule inspects: one layer of Common or of a module. Production scopes are whole
/// assemblies (namespace root = assembly name); self-check scopes are the violating fixtures of
/// this project (namespace root under <c>Fixtures.Modules</c>), so both run through the same rule
/// code.
/// </summary>
public sealed record LayerScope(
    Assembly Assembly,
    string NamespaceRoot,
    Layer Layer,
    string? Module,
    string ModulesNamespaceRoot,
    IReadOnlyCollection<string> OtherModules)
{
    public bool IsModule => Module is not null;

    /// <summary>Namespaces of the modules this scope must never depend on.</summary>
    public IReadOnlyList<string> OtherModuleNamespaces => [.. OtherModules.Select(module => $"{ModulesNamespaceRoot}.{module}")];

    /// <summary>Namespace of the same owner (Common or the module) in another layer.</summary>
    public string SiblingNamespace(Layer layer) =>
        IsModule ? $"{ModulesNamespaceRoot}.{Module}.{layer}" : $"{ProductionSolution.CommonNamespaceRoot}.{layer}";

    /// <summary>NetArchTest selection of the scope, for dependency rules.</summary>
    public PredicateList Select() => Types.InAssembly(Assembly).That().ResideInNamespaceStartingWith(NamespaceRoot);

    /// <summary>Reflection view of the scope, for naming, visibility and shape rules. Compiler-generated types are left out.</summary>
    public IReadOnlyList<Type> ReflectedTypes() =>
    [
        .. Assembly.GetTypes().Where(type =>
            IsInScope(type.Namespace)
            && !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
            && !type.Name.Contains('<', StringComparison.Ordinal)),
    ];

    /// <summary>Folders between the namespace root and the type, e.g. <c>["Invoices", "PayInvoice"]</c>.</summary>
    public IReadOnlyList<string> FoldersOf(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var @namespace = type.Namespace ?? string.Empty;
        return @namespace.Length <= NamespaceRoot.Length ? [] : @namespace[(NamespaceRoot.Length + 1)..].Split('.');
    }

    public override string ToString() => NamespaceRoot;

    private bool IsInScope(string? @namespace) =>
        @namespace is not null
        && (@namespace == NamespaceRoot || @namespace.StartsWith(NamespaceRoot + ".", StringComparison.Ordinal));
}
