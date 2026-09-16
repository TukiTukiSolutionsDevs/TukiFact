namespace TukiFact.Common.Infrastructure.Persistence;

/// <summary>
/// Thrown by <see cref="TransactionManager.BeginTransactionAsync"/> and <see cref="ReadDbConnection.OpenAsync"/>
/// when neither a tenant nor an active <c>ISystemScope</c> is present in the current scope, and by
/// <see cref="TransactionRequiredConnectionInterceptor"/> when a module context opens a connection outside the
/// kernel transaction. This is the fail-closed default (ADR-003): a missing tenant context is a programming error,
/// not a client error, so it is surfaced loudly instead of silently returning zero rows.
/// </summary>
public sealed class TenantContextMissingException : InvalidOperationException
{
    private const string DefaultMessage =
        "No tenant is present in the current scope and no system scope was entered; the operation is rejected (fail closed).";

    public TenantContextMissingException()
        : this(DefaultMessage)
    {
    }

    public TenantContextMissingException(string message)
        : base(message)
    {
    }
}
