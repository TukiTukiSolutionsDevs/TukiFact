namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

/// <summary>Clock shared by the collection; tests in one collection run sequentially, so each test sets it before acting.</summary>
public sealed class MutableTimeProvider : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 9, 16, 15, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
