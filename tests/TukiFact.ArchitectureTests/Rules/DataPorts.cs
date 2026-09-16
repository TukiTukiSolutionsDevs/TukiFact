namespace TukiFact.ArchitectureTests.Rules;

/// <summary>Persistence ports by convention: <c>I&lt;Aggregate&gt;Repository</c>.</summary>
internal static class DataPorts
{
    public static IEnumerable<Type> RepositoryPorts(IEnumerable<LayerScope> scopes) =>
        scopes
            .SelectMany(scope => scope.ReflectedTypes())
            .Where(type => type.IsInterface && type.NameWithoutArity().EndsWith("Repository", StringComparison.Ordinal));
}
