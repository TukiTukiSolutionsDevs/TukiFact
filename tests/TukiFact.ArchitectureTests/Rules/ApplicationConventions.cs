using TukiFact.Common.Application.Abstractions;

namespace TukiFact.ArchitectureTests.Rules;

/// <summary>Application-layer shape rules.</summary>
public static class ApplicationConventions
{
    /// <summary>A request (<c>*Command</c>/<c>*Query</c>) is the module's public contract: <c>public sealed</c>.</summary>
    public static IReadOnlyList<string> RequestsArePublicSealed(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var violations = scope.ReflectedTypes()
            .Where(IsRequest)
            .Where(type => !(type.IsPublic && type.IsSealed))
            .Select(type => type.FullName!);

        return RuleResults.Sorted(violations);
    }

    /// <summary>A handler/validator is an implementation detail of its request: never called directly outside the module.</summary>
    public static IReadOnlyList<string> HandlersAndValidatorsAreInternalSealed(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var violations = scope.ReflectedTypes()
            .Where(IsHandlerOrValidator)
            .Where(type => !(type.IsInternal() && type.IsSealed))
            .Select(type => type.FullName!);

        return RuleResults.Sorted(violations);
    }

    /// <summary>A handler/validator sits in the same namespace as the request it serves — easy to find, easy to delete together.</summary>
    public static IReadOnlyList<string> HandlersAndValidatorsAreNextToTheirRequest(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var types = scope.ReflectedTypes();
        var violations = new List<string>();
        foreach (var handler in types.Where(IsHandlerOrValidator))
        {
            var requestName = RequestNameOf(handler);
            var request = types.FirstOrDefault(candidate => candidate.Name == requestName);
            if (request is not null && request.Namespace != handler.Namespace)
            {
                violations.Add(handler.FullName!);
            }
        }

        return RuleResults.Sorted(violations);
    }

    /// <summary>A query handler reads; it never opens a write transaction or reaches for a write-side repository port.</summary>
    public static IReadOnlyList<string> QueryHandlersDoNotUseTransactionManagerOrRepositoryPorts(LayerScope scope, IEnumerable<LayerScope> portScopes)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(portScopes);

        string[] forbidden = [typeof(ITransactionManager).FullName!, .. DataPorts.RepositoryPorts(portScopes).Select(type => type.FullName!)];

        return RuleResults.FailingTypes(
            scope.Select().And().HaveNameEndingWith("QueryHandler").ShouldNot().HaveDependencyOnAny(forbidden));
    }

    private static bool IsRequest(Type type) =>
        type.IsConcreteClass() && (type.Name.EndsWith("Command", StringComparison.Ordinal) || type.Name.EndsWith("Query", StringComparison.Ordinal));

    private static bool IsHandlerOrValidator(Type type) =>
        type.IsConcreteClass()
        && (type.Name.EndsWith("CommandHandler", StringComparison.Ordinal)
            || type.Name.EndsWith("QueryHandler", StringComparison.Ordinal)
            || type.Name.EndsWith("Validator", StringComparison.Ordinal));

    private static string RequestNameOf(Type handler) => handler.Name switch
    {
        { } name when name.EndsWith("Handler", StringComparison.Ordinal) => name[..^"Handler".Length],
        { } name when name.EndsWith("Validator", StringComparison.Ordinal) => name[..^"Validator".Length],
        { } name => name,
    };
}
