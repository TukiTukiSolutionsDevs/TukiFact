using TukiFact.Common.Infrastructure.Persistence;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class RecordingTransactionParticipant : ITransactionParticipant
{
    public int AfterCommitCalls { get; private set; }

    public int AfterRollbackCalls { get; private set; }

    public Task AfterCommitAsync()
    {
        AfterCommitCalls++;
        return Task.CompletedTask;
    }

    public Task AfterRollbackAsync()
    {
        AfterRollbackCalls++;
        return Task.CompletedTask;
    }
}
