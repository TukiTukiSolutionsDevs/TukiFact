using TukiFact.Common.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace TukiFact.Common.Infrastructure.Persistence;

/// <summary>
/// Scoped <see cref="ISystemScope"/>: one DI scope is one job run or one request, so the bypass never
/// outlives the work that requested it. Entering the scope sets an audited, transaction-local
/// <c>app.bypass_rls</c> GUC (via <see cref="TransactionManager"/> and <see cref="ReadDbConnection"/>) and logs a
/// structured audit entry (reason, user, correlation id) through <see cref="ILogger"/>. Audit persistence in a
/// durable table (<c>common.system_scope_audits</c>) lands with the Platform module; for now the log entry is the
/// audit trail. No nesting: a second <see cref="Enter"/> while already active throws, and the check-and-set is
/// atomic so two concurrent callers on one scope can never both be admitted.
/// </summary>
internal sealed class SystemScope(
    ICurrentUser currentUser,
    ICorrelationIdAccessor correlationIdAccessor,
    ILogger<SystemScope> logger) : ISystemScope
{
    private const int Inactive = 0;
    private const int Active = 1;

    private int _state = Inactive;

    public bool IsActive => Volatile.Read(ref _state) == Active;

    public string? Reason { get; private set; }

    public IDisposable Enter(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Interlocked.CompareExchange(ref _state, Active, Inactive) != Inactive)
        {
            throw new InvalidOperationException("A system scope is already active in this execution scope; nesting is not allowed.");
        }

        Reason = reason;

        logger.SystemScopeEntered(reason, currentUser.UserId, currentUser.TenantId, correlationIdAccessor.CorrelationId);

        return new ScopeToken(this);
    }

    private void Exit()
    {
        Reason = null;
        Interlocked.Exchange(ref _state, Inactive);
    }

    private sealed class ScopeToken(SystemScope owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            owner.Exit();
            _disposed = true;
        }
    }
}
