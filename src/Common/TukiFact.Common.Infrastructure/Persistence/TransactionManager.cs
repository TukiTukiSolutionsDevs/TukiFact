using System.Data;
using TukiFact.Common.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace TukiFact.Common.Infrastructure.Persistence;

/// <summary>
/// Scoped <see cref="ITransactionManager"/> used by <c>TransactionBehavior</c>. Every module
/// <see cref="BaseDbContext"/> resolved in the scope shares one Npgsql connection (one database, one schema
/// per module) and enlists here, so a command uses a single connection and a single transaction whichever
/// module contexts it touches. Commit saves each enlisted context (dispatching domain events before the
/// database commit) and then commits; any failure before the database commit rolls back and surfaces the original
/// exception. Once the database has committed, cleanup and participant failures are logged, never thrown: the
/// caller must not be told a durable command failed.
/// </summary>
/// <remarks>
/// RLS tenant hook (ADR-003): right after opening the transaction and before any context is enlisted, this
/// sets the transaction-local GUCs <c>app.current_tenant</c> and <c>app.bypass_rls</c> from
/// <see cref="ICurrentUser.TenantId"/> and <see cref="ISystemScope.IsActive"/>, always parameterised via
/// <c>set_config</c> — never string interpolation. The GUC is set unconditionally, including the fail-closed
/// case (empty tenant, bypass off), so the database-level defense is in place even before the app-level
/// defense (<see cref="TenantContextMissingException"/>) is evaluated and thrown.
/// </remarks>
internal sealed class TransactionManager(
    NpgsqlDataSource dataSource,
    ICurrentUser currentUser,
    ISystemScope systemScope,
    ICorrelationIdAccessor correlationIdAccessor,
    ILogger<TransactionManager> logger)
    : ITransactionManager, IAsyncDisposable
{
    private const string TenantContextPath = "TransactionManager.BeginTransactionAsync";

    private readonly List<BaseDbContext> _contexts = [];
    private readonly List<ITransactionParticipant> _participants = [];
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;

    public NpgsqlConnection Connection => _connection ??= dataSource.CreateConnection();

    /// <summary>The open command transaction.</summary>
    public NpgsqlTransaction? CurrentTransaction => _transaction;

    /// <summary>Notifies <paramref name="participant"/> once the current transaction commits or rolls back.</summary>
    public void Enlist(ITransactionParticipant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);

        if (_transaction is null)
        {
            throw new InvalidOperationException("There is no transaction in progress to participate in.");
        }

        if (!_participants.Contains(participant))
        {
            _participants.Add(participant);
        }
    }

    public void Enlist(BaseDbContext context)
    {
        _contexts.Add(context);

        if (_transaction is not null)
        {
            context.Database.UseTransaction(_transaction);
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already in progress in this scope.");
        }

        if (Connection.State != ConnectionState.Open)
        {
            await Connection.OpenAsync(cancellationToken);
        }

        _transaction = await Connection.BeginTransactionAsync(cancellationToken);

        await SetTenantContextAsync(cancellationToken);

        foreach (var context in _contexts)
        {
            await context.Database.UseTransactionAsync(_transaction, cancellationToken);
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _transaction ?? throw new InvalidOperationException("There is no transaction in progress to commit.");

        try
        {
            // Index loop: domain event handlers dispatched by SaveChanges may resolve, and enlist, another context.
            for (var index = 0; index < _contexts.Count; index++)
            {
                await _contexts[index].SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                logger.RollbackFailed(rollbackException);
            }

            throw;
        }

        try
        {
            await EndTransactionAsync(clearTracking: false);
        }
        catch (Exception exception)
        {
            logger.CommitCleanupFailed(exception);
        }

        await NotifyParticipantsAsync(committed: true);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            try
            {
                await EndTransactionAsync(clearTracking: true);
            }
            catch (Exception exception)
            {
                logger.RollbackCleanupFailed(exception);
            }

            await NotifyParticipantsAsync(committed: false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    /// <summary>
    /// Sets the transaction-local tenant GUCs and, when neither a tenant nor an active system scope is present,
    /// rolls back and throws <see cref="TenantContextMissingException"/> — the GUC is set first regardless, so a
    /// row is never visible even if a future code path skipped the exception.
    /// </summary>
    private async Task SetTenantContextAsync(CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        var bypass = systemScope.IsActive;

        await using (var command = TenantContextSql.CreateSetCommand(Connection, _transaction, tenantId, bypass, transactionLocal: true))
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        if (tenantId is null && !bypass)
        {
            logger.TenantContextMissing(correlationIdAccessor.CorrelationId, TenantContextPath);
            await RollbackAsync(CancellationToken.None);
            throw new TenantContextMissingException();
        }
    }

    private async Task NotifyParticipantsAsync(bool committed)
    {
        var participants = _participants.ToArray();
        _participants.Clear();

        foreach (var participant in participants)
        {
            try
            {
                await (committed ? participant.AfterCommitAsync() : participant.AfterRollbackAsync());
            }
            catch (Exception exception)
            {
                logger.TransactionParticipantFailed(participant.GetType(), committed ? "AfterCommit" : "AfterRollback", exception);
            }
        }
    }

    private async Task EndTransactionAsync(bool clearTracking)
    {
        foreach (var context in _contexts)
        {
            await context.Database.UseTransactionAsync(null);

            if (clearTracking)
            {
                context.ChangeTracker.Clear();
            }
        }

        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        await Connection.CloseAsync();
    }
}
