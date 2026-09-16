using TukiFact.Common.Infrastructure.Persistence;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class ThrowingAfterCommitParticipant : ITransactionParticipant
{
    public InvalidOperationException Failure { get; } = new("Outbox release failed after commit.");

    public Task AfterCommitAsync() => Task.FromException(Failure);

    public Task AfterRollbackAsync() => Task.CompletedTask;
}
