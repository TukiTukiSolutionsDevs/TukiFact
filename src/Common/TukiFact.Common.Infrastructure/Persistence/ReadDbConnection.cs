using System.Data;
using System.Data.Common;
using TukiFact.Common.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace TukiFact.Common.Infrastructure.Persistence;

/// <summary>
/// Base adapter for <c>I&lt;Module&gt;ReadDbConnection</c> ports (query side): one lazily created connection per
/// scope that Dapper opens and closes around each call. No EF Core, no tracking, no transaction.
/// </summary>
/// <remarks>
/// RLS tenant hook (ADR-003/ADR-008): <see cref="OpenAsync"/> is the only way to obtain the connection, so Dapper
/// can never receive an unprimed connection. Opening sets the session-level GUCs <c>app.current_tenant</c> and
/// <c>app.bypass_rls</c> (parameterised, never interpolated) from <see cref="ICurrentUser"/> and
/// <see cref="ISystemScope"/>; disposing resets both before the physical connection returns to the pool, so a
/// reused pooled connection never carries a residual tenant into the next caller.
/// </remarks>
public abstract class ReadDbConnection(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    ISystemScope systemScope,
    ICorrelationIdAccessor correlationIdAccessor,
    ILogger<ReadDbConnection> logger)
    : IDisposable, IAsyncDisposable
{
    private const string TenantContextPath = "ReadDbConnection.OpenAsync";

    private DbConnection? _connection;
    private bool _disposed;
    private bool _tenantContextSet;

    /// <summary>
    /// Opens the connection (if not already open) and primes the session-level tenant GUCs. Throws
    /// <see cref="TenantContextMissingException"/> before opening when neither a tenant nor an active system
    /// scope is present.
    /// </summary>
    public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var connection = _connection ??= connectionFactory.CreateConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
            _tenantContextSet = false;
        }

        if (!_tenantContextSet)
        {
            await SetTenantContextAsync(connection, cancellationToken);
            _tenantContextSet = true;
        }

        return connection;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            if (_connection is { State: ConnectionState.Open })
            {
                ResetTenantContext(_connection);
            }

            _connection?.Dispose();
            _connection = null;
        }

        _disposed = true;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_connection is { State: ConnectionState.Open } connection)
        {
            await ResetTenantContextAsync(connection, CancellationToken.None);
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private async Task SetTenantContextAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        var bypass = systemScope.IsActive;

        if (tenantId is null && !bypass)
        {
            logger.TenantContextMissing(correlationIdAccessor.CorrelationId, TenantContextPath);
            await ResetTenantContextAsync(connection, cancellationToken);
            throw new TenantContextMissingException();
        }

        await using var command = TenantContextSql.CreateSetCommand((NpgsqlConnection)connection, transaction: null, tenantId, bypass, transactionLocal: false);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ResetTenantContextAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new NpgsqlCommand(TenantContextSql.ResetSession, (NpgsqlConnection)connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (NpgsqlException)
        {
            // The connection may already be broken; the pool discards broken connections instead of reusing
            // them, so a residual GUC can never leak through this path either way.
        }
    }

    private static void ResetTenantContext(DbConnection connection)
    {
        try
        {
            using var command = new NpgsqlCommand(TenantContextSql.ResetSession, (NpgsqlConnection)connection);
            command.ExecuteNonQuery();
        }
        catch (NpgsqlException)
        {
            // See the async overload: a broken connection is discarded by the pool, not reused.
        }
    }
}
