using TukiFact.BuildingBlocks.Contracts;
using TukiFact.Common.Domain.DomainEvents;

namespace TukiFact.ArchitectureTests.Rules;

/// <summary>Event-shape rules.</summary>
public static class MessagingConventions
{
    /// <summary>An integration event is a published contract: it lives only in <c>BuildingBlocks.Contracts</c>, never in Common or a module.</summary>
    public static IReadOnlyList<string> IntegrationEventsLiveOnlyInContracts(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var violations = scope.ReflectedTypes()
            .Where(type => typeof(IIntegrationEvent).IsAssignableFrom(type) && type.IsConcreteClass())
            .Select(type => type.FullName!);

        return RuleResults.Sorted(violations);
    }

    /// <summary>An integration event is named <c>&lt;Noun&gt;&lt;PastVerb&gt;IntegrationEvent</c>.</summary>
    public static IReadOnlyList<string> IntegrationEventNamesFollowThePattern(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var violations = scope.ReflectedTypes()
            .Where(type => typeof(IIntegrationEvent).IsAssignableFrom(type) && type.IsConcreteClass())
            .Where(type => !FollowsPattern(type.Name, "IntegrationEvent"))
            .Select(type => type.FullName!);

        return RuleResults.Sorted(violations);
    }

    /// <summary>A domain event is named <c>&lt;Noun&gt;&lt;PastVerb&gt;DomainEvent</c>.</summary>
    public static IReadOnlyList<string> DomainEventNamesFollowThePattern(LayerScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var violations = scope.ReflectedTypes()
            .Where(type => typeof(IDomainEvent).IsAssignableFrom(type) && type.IsConcreteClass())
            .Where(type => !FollowsPattern(type.Name, "DomainEvent"))
            .Select(type => type.FullName!);

        return RuleResults.Sorted(violations);
    }

    private static bool FollowsPattern(string name, string suffix) =>
        name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal);
}
