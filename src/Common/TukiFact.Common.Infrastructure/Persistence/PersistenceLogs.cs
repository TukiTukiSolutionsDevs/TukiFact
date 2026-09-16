using Microsoft.Extensions.Logging;

namespace TukiFact.Common.Infrastructure.Persistence;

internal static partial class PersistenceLogs
{
    [LoggerMessage(EventId = 7100, Level = LogLevel.Warning, Message = "Optimistic concurrency conflict on {EntryCount} entries")]
    public static partial void ConcurrencyConflict(this ILogger logger, int entryCount);

    [LoggerMessage(
        EventId = 7101,
        Level = LogLevel.Warning,
        Message = "System scope entered: reason={Reason}, userId={UserId}, tenantId={TenantId}, correlationId={CorrelationId}")]
    public static partial void SystemScopeEntered(
        this ILogger logger,
        string reason,
        Guid? userId,
        Guid? tenantId,
        string correlationId);

    [LoggerMessage(
        EventId = 7102,
        Level = LogLevel.Warning,
        Message = "Tenant context missing at {Path}: no tenant and no active system scope, failing closed (correlationId={CorrelationId})")]
    public static partial void TenantContextMissing(this ILogger logger, string correlationId, string path);

    [LoggerMessage(EventId = 7103, Level = LogLevel.Error, Message = "Post-commit cleanup failed; the transaction is already committed")]
    public static partial void CommitCleanupFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 7104, Level = LogLevel.Error, Message = "Transaction participant {ParticipantType} failed in {Hook}")]
    public static partial void TransactionParticipantFailed(this ILogger logger, Type participantType, string hook, Exception exception);

    [LoggerMessage(EventId = 7105, Level = LogLevel.Error, Message = "Rollback failed after a commit error; the original exception is rethrown")]
    public static partial void RollbackFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 7106, Level = LogLevel.Error, Message = "Post-rollback cleanup failed")]
    public static partial void RollbackCleanupFailed(this ILogger logger, Exception exception);
}
