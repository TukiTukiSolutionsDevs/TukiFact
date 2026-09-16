using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Application.Behaviors;

/// <summary>
/// Wraps commands (<see cref="ICommand"/> or <c>*Command</c>) in a transaction: commit on success,
/// rollback on failure or exception. Queries pass through untouched.
/// </summary>
internal sealed class TransactionBehavior<TRequest, TResponse>(ITransactionManager transactionManager)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!IsCommand(typeof(TRequest)))
        {
            return await next();
        }

        await transactionManager.BeginTransactionAsync(cancellationToken);

        TResponse response;
        try
        {
            response = await next();
        }
        catch
        {
            await transactionManager.RollbackAsync(CancellationToken.None);
            throw;
        }

        // CancellationToken.None: a token cancelled after the handler returned must not leave the transaction open.
        if (response.IsSuccess)
        {
            await transactionManager.CommitAsync(CancellationToken.None);
        }
        else
        {
            await transactionManager.RollbackAsync(CancellationToken.None);
        }

        return response;
    }

    private static bool IsCommand(Type requestType) =>
        typeof(ICommand).IsAssignableFrom(requestType)
        || requestType.Name.EndsWith(UseCaseNaming.CommandSuffix, StringComparison.Ordinal);
}
