namespace TukiFact.ArchitectureTests.Rules;

/// <summary>Folder/namespace shape rules.</summary>
public static class StructureConventions
{
    /// <summary>Technical-role folders group by "what kind of class", not "what business concept" — banned everywhere.</summary>
    private static readonly string[] BannedFolders = ["Handlers", "Services", "Managers", "Helpers", "Dtos", "Repositories"];

    /// <summary>
    /// No horizontal (technical-role) folders anywhere in the layer, and <c>Shared</c> is only valid
    /// nested inside a submodule folder — never directly under the layer root.
    /// </summary>
    public static IReadOnlyList<string> DoesNotUseHorizontalFolders(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var violations = scope.ReflectedTypes()
            .Where(type =>
            {
                var folders = scope.FoldersOf(type);
                return folders.Any(folder => BannedFolders.Contains(folder, StringComparer.Ordinal))
                    || (folders.Count > 0 && folders[0] == "Shared");
            })
            .Select(type => type.FullName!);

        return RuleResults.Sorted(violations);
    }
}
