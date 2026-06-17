using MarketData.Application.Contracts;

namespace MarketData.Application.Calendar;

/// <summary>
/// Computed US-equity (NYSE) holiday and early-close rules. Pure date math — no clock.
/// Produces named <see cref="Holiday"/> entries. Ad-hoc closures live in
/// <see cref="UsEquityClosures"/>; per-year corrections come from JSON overrides.
/// </summary>
internal static class UsEquityHolidayRules
{
    /// <summary>The computed full-closure holidays for a year (observed dates applied).</summary>
    public static IEnumerable<Holiday> Holidays(int year)
    {
        yield return Full(Observe(new DateOnly(year, 1, 1)), "New Year's Day");
        yield return Full(NthWeekday(year, 1, DayOfWeek.Monday, 3), "Martin Luther King Jr. Day");
        yield return Full(NthWeekday(year, 2, DayOfWeek.Monday, 3), "Washington's Birthday");
        yield return Full(GoodFriday(year), "Good Friday");
        yield return Full(LastWeekday(year, 5, DayOfWeek.Monday), "Memorial Day");
        if (year >= 2022)
            yield return Full(Observe(new DateOnly(year, 6, 19)), "Juneteenth National Independence Day");
        yield return Full(Observe(new DateOnly(year, 7, 4)), "Independence Day");
        yield return Full(NthWeekday(year, 9, DayOfWeek.Monday, 1), "Labor Day");
        yield return Full(NthWeekday(year, 11, DayOfWeek.Thursday, 4), "Thanksgiving Day");
        yield return Full(Observe(new DateOnly(year, 12, 25)), "Christmas Day");
    }

    /// <summary>The computed early-close (half) days for a year.</summary>
    public static IEnumerable<Holiday> EarlyCloses(int year)
    {
        yield return Early(NthWeekday(year, 11, DayOfWeek.Thursday, 4).AddDays(1), "Day after Thanksgiving");
        yield return Early(new DateOnly(year, 12, 24), "Christmas Eve");
    }

    private static Holiday Full(DateOnly date, string name) => new(date, name, IsEarlyClose: false);
    private static Holiday Early(DateOnly date, string name) => new(date, name, IsEarlyClose: true);

    /// <summary>Shifts a fixed-date holiday to the observed weekday (Sat → Fri, Sun → Mon).</summary>
    public static DateOnly Observe(DateOnly holiday) => holiday.DayOfWeek switch
    {
        DayOfWeek.Saturday => holiday.AddDays(-1),
        DayOfWeek.Sunday => holiday.AddDays(1),
        _ => holiday
    };

    /// <summary>The nth occurrence (1-based) of a weekday in a month.</summary>
    public static DateOnly NthWeekday(int year, int month, DayOfWeek weekday, int nth)
    {
        var first = new DateOnly(year, month, 1);
        int offset = ((int)weekday - (int)first.DayOfWeek + 7) % 7;
        return first.AddDays(offset + (nth - 1) * 7);
    }

    /// <summary>The last occurrence of a weekday in a month.</summary>
    public static DateOnly LastWeekday(int year, int month, DayOfWeek weekday)
    {
        var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        int offset = ((int)last.DayOfWeek - (int)weekday + 7) % 7;
        return last.AddDays(-offset);
    }

    /// <summary>Good Friday: Easter Sunday (Gregorian Computus) minus two days.</summary>
    public static DateOnly GoodFriday(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateOnly(year, month, day).AddDays(-2);
    }
}
