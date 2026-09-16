namespace TukiFact.Common.Infrastructure.Persistence;

/// <summary>
/// Work that follows the outcome of the scope transaction owned by <see cref="TransactionManager"/>, such as releasing
/// an integration event outbox after commit or discarding it after rollback. Implementations must not throw.
/// </summary>
internal interface ITransactionParticipant
{
    Task AfterCommitAsync();

    Task AfterRollbackAsync();
}
