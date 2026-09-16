using TukiFact.Common.Infrastructure.Messaging.Correlation;
using TukiFact.Common.Infrastructure.Persistence;
using TukiFact.Common.IntegrationTests.Persistence.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace TukiFact.Common.IntegrationTests.Persistence;

/// <summary>Admission contract of the audited bypass; no database is contacted.</summary>
public sealed class SystemScopeTests
{
    private const int Attempts = 2_000;

    [Fact]
    public async Task Enter_TwoConcurrentCallersOnSameInstance_AdmitsExactlyOne()
    {
        // Arrange
        var admissionsPerAttempt = new List<int>(Attempts);

        // Act
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            var scope = new SystemScope(new TestCurrentUser(), new CorrelationIdAccessor(), NullLogger<SystemScope>.Instance);
            using var barrier = new Barrier(participantCount: 2);
            var outcomes = await Task.WhenAll(TryEnterAsync(scope, barrier), TryEnterAsync(scope, barrier));
            admissionsPerAttempt.Add(outcomes.Count(admitted => admitted));
        }

        // Assert
        admissionsPerAttempt.Should().OnlyContain(admissions => admissions == 1, "the check-and-set must be atomic");
    }

    private static Task<bool> TryEnterAsync(SystemScope scope, Barrier barrier) => Task.Run(() =>
    {
        barrier.SignalAndWait();
        try
        {
            scope.Enter("concurrent-probe");
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    });
}
