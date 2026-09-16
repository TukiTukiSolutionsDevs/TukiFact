using System.Collections.Concurrent;
using TukiFact.Common.Application.Abstractions;

namespace TukiFact.Common.UnitTests.Fakes;

internal sealed class FakeTransactionManager : ITransactionManager
{
    public const string Begin = "begin";
    public const string Commit = "commit";
    public const string Rollback = "rollback";

    private readonly ConcurrentQueue<string> _calls = new();

    public IReadOnlyList<string> Calls => [.. _calls];

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Record(Begin, cancellationToken);

    public Task CommitAsync(CancellationToken cancellationToken = default) => Record(Commit, cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) => Record(Rollback, cancellationToken);

    private Task Record(string call, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _calls.Enqueue(call);
        return Task.CompletedTask;
    }
}
