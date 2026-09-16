using TukiFact.Common.Infrastructure.Persistence;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class ThrowingAfterRollbackParticipant : ITransactionParticipant
{
    public InvalidOperationException Failure { get; } = new("Outbox discard failed after rollback.");

    public Task AfterCommitAsync() => Task.CompletedTask;

    public Task AfterRollbackAsync() => Task.FromException(Failure);
}
