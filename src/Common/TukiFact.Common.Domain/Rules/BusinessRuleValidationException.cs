namespace TukiFact.Common.Domain.Rules;

/// <summary>
/// Signals a broken domain invariant. Predictable use-case failures use <c>Result.Failure</c> instead.
/// </summary>
public sealed class BusinessRuleValidationException : Exception
{
    public BusinessRuleValidationException(IBusinessRule brokenRule)
        : base((brokenRule ?? throw new ArgumentNullException(nameof(brokenRule))).Message)
    {
        BrokenRule = brokenRule;
    }

    public IBusinessRule BrokenRule { get; }
}
