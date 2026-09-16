using TukiFact.Common.Infrastructure.Modules;
using TukiFact.Common.Presentation.Endpoints;

namespace TukiFact.ArchitectureTests.Rules;

/// <summary>Module composition-root rules.</summary>
public static class CompositionConventions
{
    /// <summary>Exactly one <see cref="IModule"/> per module, named <c>&lt;Module&gt;Module</c>, at the Infrastructure root.</summary>
    public static IReadOnlyList<string> ExactlyOneModuleAtTheExpectedName(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (!scope.IsModule)
        {
            return [];
        }

        var expectedName = $"{scope.Module}Module";
        var implementations = scope.ReflectedTypes()
            .Where(type => typeof(IModule).IsAssignableFrom(type) && type.IsConcreteClass())
            .ToArray();

        IReadOnlyList<string> violations = implementations.Length switch
        {
            0 => [$"{scope.NamespaceRoot}: no IModule implementation found (expected {expectedName})"],
            1 when implementations[0].Name == expectedName && implementations[0].Namespace == scope.NamespaceRoot => [],
            1 => [$"{implementations[0].FullName}: expected exactly one IModule named {expectedName} at the Infrastructure root"],
            _ => [.. implementations.Select(type => $"{type.FullName}: more than one IModule implementation in the module")],
        };

        return RuleResults.Sorted(violations);
    }

    /// <summary>An <see cref="IEndpoint"/> is <c>sealed &lt;UseCase&gt;Endpoint</c>, mapped under a submodule folder — never directly at the Presentation root.</summary>
    public static IReadOnlyList<string> EndpointsAreSealedNamedByUseCaseAndUnderASubmodule(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var violations = scope.ReflectedTypes()
            .Where(type => typeof(IEndpoint).IsAssignableFrom(type) && type.IsConcreteClass())
            .Where(type => !(type.IsSealed && type.Name.EndsWith("Endpoint", StringComparison.Ordinal) && scope.FoldersOf(type).Count > 0))
            .Select(type => type.FullName!);

        return RuleResults.Sorted(violations);
    }
}
