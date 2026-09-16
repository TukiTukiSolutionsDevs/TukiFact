using System.Runtime.CompilerServices;
using TukiFact.Common.Domain.Rules;

namespace TukiFact.Common.Domain.Entities;

/// <summary>Entity with identity equality: same concrete type and same non-default id.</summary>
public abstract class Entity<TId>
    where TId : notnull
{
    public TId Id { get; protected init; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other || other.GetType() != GetType())
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return !IsTransient() && !other.IsTransient() && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() =>
        IsTransient() ? RuntimeHelpers.GetHashCode(this) : HashCode.Combine(GetType(), Id);

    /// <summary>Enforces an invariant; a broken rule throws <see cref="BusinessRuleValidationException"/>.</summary>
    protected static void CheckRule(IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (rule.IsBroken())
        {
            throw new BusinessRuleValidationException(rule);
        }
    }

    private bool IsTransient() => EqualityComparer<TId>.Default.Equals(Id, default);
}
