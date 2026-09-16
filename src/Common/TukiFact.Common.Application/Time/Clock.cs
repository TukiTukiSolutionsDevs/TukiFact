using TukiFact.Common.Application.Abstractions;

namespace TukiFact.Common.Application.Time;

internal sealed class Clock : IClock
{
    private const string LimaTimeZoneId = "America/Lima";

    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _limaTimeZone;

    public Clock(TimeProvider timeProvider)
        : this(timeProvider, () => TimeZoneInfo.FindSystemTimeZoneById(LimaTimeZoneId))
    {
    }

    /// <summary>Resolver seam for tests; hosts without tz data (trimmed containers) get the fixed fallback.</summary>
    internal Clock(TimeProvider timeProvider, Func<TimeZoneInfo> limaTimeZoneResolver)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(limaTimeZoneResolver);

        _timeProvider = timeProvider;
        _limaTimeZone = ResolveLimaTimeZone(limaTimeZoneResolver);
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public DateTimeOffset LimaNow => TimeZoneInfo.ConvertTime(UtcNow, _limaTimeZone);

    private static TimeZoneInfo ResolveLimaTimeZone(Func<TimeZoneInfo> resolver)
    {
        try
        {
            return resolver();
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Peru has no daylight saving time, so a fixed UTC-5 zone is exact.
            return TimeZoneInfo.CreateCustomTimeZone(LimaTimeZoneId, TimeSpan.FromHours(-5), "Peru Time", "Peru Time");
        }
    }
}
