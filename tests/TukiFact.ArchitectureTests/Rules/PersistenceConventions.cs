using System.Reflection;
using TukiFact.Common.Infrastructure.Persistence;

namespace TukiFact.ArchitectureTests.Rules;

/// <summary>Persistence-shape rules.</summary>
public static class PersistenceConventions
{
    private const int MaxRepositoryPublicMethods = 5;

    private static readonly string[] ReservedShadowPropertyNames =
    [
        ShadowProperties.CreatedAt,
        ShadowProperties.CreatedBy,
        ShadowProperties.UpdatedAt,
        ShadowProperties.UpdatedBy,
        ShadowProperties.SyncVersion,
    ];

    /// <summary>A repository is a narrow port, not a generic data-access facade.</summary>
    public static IReadOnlyList<string> RepositoriesHaveAtMostFivePublicMethods(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var violations = RepositoryTypes(scope)
            .Where(type => PublicMethodCount(type) > MaxRepositoryPublicMethods)
            .Select(type => type.FullName!);

        return RuleResults.Sorted(violations);
    }

    /// <summary>A repository returns materialized results; it never leaks a composable, provider-specific query.</summary>
    public static IReadOnlyList<string> RepositoriesNeverExposeIQueryable(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var violations = RepositoryTypes(scope)
            .Where(UsesIQueryable)
            .Select(type => type.FullName!);

        return RuleResults.Sorted(violations);
    }

    /// <summary>No generic <c>IRepository&lt;T&gt;</c> and no <c>IUnitOfWork</c> — every port is named for what it does, and a transaction is scoped by <c>ITransactionManager</c>.</summary>
    public static IReadOnlyList<string> NoGenericRepositoryOrUnitOfWork(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var violations = scope.ReflectedTypes()
            .Where(type => type.NameWithoutArity() is "IRepository" or "IUnitOfWork" or "UnitOfWork")
            .Select(type => type.FullName!);

        return RuleResults.Sorted(violations);
    }

    /// <summary>Exactly one <c>&lt;Module&gt;DbContext</c> at the Infrastructure root of a module.</summary>
    public static IReadOnlyList<string> OneDbContextPerModule(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        if (!scope.IsModule)
        {
            return [];
        }

        var expectedName = $"{scope.Module}DbContext";
        var dbContexts = scope.ReflectedTypes().Where(type => type.Name.EndsWith("DbContext", StringComparison.Ordinal)).ToArray();

        IReadOnlyList<string> violations = dbContexts.Length switch
        {
            0 => [$"{scope.NamespaceRoot}: no <Module>DbContext found (expected {expectedName})"],
            1 when dbContexts[0].Name != expectedName => [$"{dbContexts[0].FullName}: expected exactly {expectedName}"],
            1 => [],
            _ => [.. dbContexts.Select(type => $"{type.FullName}: more than one DbContext in the module")],
        };

        return RuleResults.Sorted(violations);
    }

    /// <summary>A Domain entity never declares a shadow property name that <c>BaseDbContext</c> owns.</summary>
    public static IReadOnlyList<string> EntitiesDoNotDeclareReservedShadowPropertyNames(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var violations = scope.ReflectedTypes()
            .Where(type => type.IsConcreteClass())
            .SelectMany(type => type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(property => ReservedShadowPropertyNames.Contains(property.Name, StringComparer.Ordinal))
                .Select(property => $"{type.FullName}.{property.Name}"));

        return RuleResults.Sorted(violations);
    }

    private static IEnumerable<Type> RepositoryTypes(LayerScope scope) =>
        scope.ReflectedTypes().Where(type => type.NameWithoutArity().EndsWith("Repository", StringComparison.Ordinal));

    private static int PublicMethodCount(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Count(method => !method.IsSpecialName);

    private static bool UsesIQueryable(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(method => IsOrIsQueryable(method.ReturnType) || method.GetParameters().Any(parameter => IsOrIsQueryable(parameter.ParameterType)));

    private static bool IsOrIsQueryable(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>);
}
