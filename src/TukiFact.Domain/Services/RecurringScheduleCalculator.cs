namespace TukiFact.Domain.Services;

/// <summary>
/// Pure date arithmetic for recurring invoice schedules.
/// Lives in Domain so the controller (resume flow) and scheduler (advance flow)
/// share one source of truth instead of each rolling their own.
/// </summary>
public static class RecurringScheduleCalculator
{
    /// <summary>
    /// Returns the next emission date strictly after <paramref name="from"/>, given the schedule's
    /// frequency and (optional) day-of-month / day-of-week. Used when resuming a paused schedule
    /// — never returns <paramref name="from"/> itself, so a resume does not trigger an
    /// immediate emission within the next scheduler tick.
    /// </summary>
    public static DateOnly NextAfter(DateOnly from, string frequency, int? dayOfMonth, int? dayOfWeek, DateOnly startDate)
    {
        return frequency switch
        {
            "daily" => from.AddDays(1),
            "weekly" => NextWeekly(from, dayOfWeek ?? (int)startDate.DayOfWeek),
            "biweekly" => from.AddDays(14),
            "monthly" => NextMonthly(from, dayOfMonth ?? startDate.Day),
            "yearly" => from.AddYears(1),
            _ => from.AddMonths(1),
        };
    }

    /// <summary>
    /// Returns the next emission date for a schedule advancing from the previous emission.
    /// Used by the scheduler after a successful emit.
    /// </summary>
    public static DateOnly AdvanceFrom(DateOnly previous, string frequency, int? dayOfMonth, int? dayOfWeek, DateOnly startDate)
    {
        return NextAfter(previous, frequency, dayOfMonth, dayOfWeek, startDate);
    }

    private static DateOnly NextWeekly(DateOnly from, int targetDow)
    {
        var current = (int)from.DayOfWeek;
        var delta = ((targetDow - current) % 7 + 7) % 7;
        if (delta == 0) delta = 7; // strictly after
        return from.AddDays(delta);
    }

    private static DateOnly NextMonthly(DateOnly from, int targetDay)
    {
        var clamped = Math.Clamp(targetDay, 1, 28);
        var candidate = new DateOnly(from.Year, from.Month, clamped);
        if (candidate <= from) candidate = candidate.AddMonths(1);
        return candidate;
    }

    /// <summary>
    /// Lima time zone — used so "today" for due-check and IssueDate matches Peruvian calendar
    /// instead of UTC, which would shift at 19:00 PET (= 00:00 UTC next day).
    /// </summary>
    public static DateOnly TodayInLima()
    {
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
        }
        catch (TimeZoneNotFoundException)
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        return DateOnly.FromDateTime(now);
    }
}
