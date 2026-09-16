namespace TukiFact.Common.Application.Abstractions;

/// <summary>
/// Explicit, audited cross-tenant execution. Background jobs, backoffice and seeds enter this scope;
/// everything else fails closed when no tenant is in context. Implemented in
/// <c>TukiFact.Common.Infrastructure</c>, consumed here so the Application layer can depend on the
/// contract without depending on persistence.
/// </summary>
public interface ISystemScope
{
    bool IsActive { get; }

    string? Reason { get; }

    /// <summary>Enters the scope. Throws if a scope is already active (no nesting).</summary>
    IDisposable Enter(string reason);
}
