namespace TukiFact.Common.Application.Abstractions;

/// <summary>
/// Transaction port consumed only by <see cref="Behaviors.TransactionBehavior{TRequest,TResponse}"/>. Handlers never
/// depend on it: there is no manual unit of work. Commit persists tracked changes and commits.
/// </summary>
public interface ITransactionManager
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
