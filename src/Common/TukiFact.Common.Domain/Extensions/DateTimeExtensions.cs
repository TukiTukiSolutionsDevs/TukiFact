namespace TukiFact.Common.Domain.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a DateTime to a Unix timestamp (seconds since Unix epoch).
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static long ToUnixTimestamp(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();
    }
}
