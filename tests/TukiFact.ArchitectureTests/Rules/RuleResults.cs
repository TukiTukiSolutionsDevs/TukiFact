using NetArchTest.Rules;

namespace TukiFact.ArchitectureTests.Rules;

/// <summary>Uniform, sorted violation lists, so a failing test prints every offender at once.</summary>
public static class RuleResults
{
    /// <summary>Full names of the types that fail <paramref name="conditions"/>.</summary>
    public static IReadOnlyList<string> FailingTypes(ConditionList conditions)
    {
        var result = conditions.GetResult();
        return result.IsSuccessful ? [] : Sorted(result.FailingTypeNames ?? []);
    }

    public static IReadOnlyList<string> Sorted(IEnumerable<string> violations) =>
        [.. violations.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
}
