using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TukiFact.Common.Infrastructure.Persistence;

/// <summary>
/// Fails loud when a module <see cref="BaseDbContext"/> tries to open the shared connection on its own: outside
/// <see cref="TransactionManager.BeginTransactionAsync"/> no tenant GUC has been set, so a query would silently
/// see zero rows. Inside a kernel transaction the connection is already open and EF never reaches this hook.
/// </summary>
internal sealed class TransactionRequiredConnectionInterceptor(TransactionManager transactions) : DbConnectionInterceptor
{
    internal const string Message = "Module DbContexts require an open kernel transaction; use the read connection for queries";

    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        EnsureTransaction();
        return result;
    }

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        EnsureTransaction();
        return ValueTask.FromResult(result);
    }

    private void EnsureTransaction()
    {
        if (transactions.CurrentTransaction is null)
        {
            throw new TenantContextMissingException(Message);
        }
    }
}
