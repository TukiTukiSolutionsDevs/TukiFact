namespace TukiFact.TestSupport;

/// <summary>Polls a condition until it is satisfied or a timeout elapses; used for assertions against work that
/// completes asynchronously relative to the test (e.g. a background dispatcher).</summary>
public static class Eventually
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(100);

    public static async Task<T> ShouldAsync<T>(
        Func<Task<T>> probe,
        Func<T, bool> isSatisfied,
        TimeSpan? timeout = null,
        TimeSpan? interval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(isSatisfied);

        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        var delay = interval ?? DefaultInterval;

        while (true)
        {
            var value = await probe();
            if (isSatisfied(value))
            {
                return value;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException("The expected condition was not satisfied within the timeout.");
            }

            await Task.Delay(delay, cancellationToken);
        }
    }
}
