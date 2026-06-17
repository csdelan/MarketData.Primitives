using MarketData.Application.Contracts;

namespace MarketData.Application.Calendar;

/// <summary>
/// Bundled table of known, historical ad-hoc NYSE closures and special early closes that
/// the computed rules cannot derive (national days of mourning, weather, emergencies).
/// <para>
/// This table covers <b>historical / known</b> events only. Future ad-hoc closures should
/// be supplied via per-year JSON overrides, which take precedence over this table.
/// </para>
/// </summary>
internal static class UsEquityClosures
{
    private static Holiday Full(int y, int m, int d, string name) => new(new DateOnly(y, m, d), name, IsEarlyClose: false);
    private static Holiday Early(int y, int m, int d, string name) => new(new DateOnly(y, m, d), name, IsEarlyClose: true);

    /// <summary>Special full-closure days indexed by year.</summary>
    private static readonly IReadOnlyList<Holiday> SpecialClosures =
    [
        // National days of mourning
        Full(2025, 1, 9, "National Day of Mourning (President Carter)"),
        Full(2018, 12, 5, "National Day of Mourning (President G. H. W. Bush)"),
        Full(2007, 1, 2, "National Day of Mourning (President Ford)"),
        Full(2004, 6, 11, "National Day of Mourning (President Reagan)"),

        // Hurricane Sandy
        Full(2012, 10, 29, "Hurricane Sandy"),
        Full(2012, 10, 30, "Hurricane Sandy"),

        // September 11 attacks (markets closed Sep 11–14, 2001)
        Full(2001, 9, 11, "September 11 Attacks"),
        Full(2001, 9, 12, "September 11 Attacks"),
        Full(2001, 9, 13, "September 11 Attacks"),
        Full(2001, 9, 14, "September 11 Attacks"),
    ];

    /// <summary>Special early-close days (representative; extend via JSON overrides).</summary>
    private static readonly IReadOnlyList<Holiday> SpecialEarlyCloses =
    [
        Early(2018, 12, 24, "Christmas Eve"),
    ];

    public static IEnumerable<Holiday> Closures(int year) =>
        SpecialClosures.Where(h => h.Date.Year == year);

    public static IEnumerable<Holiday> EarlyCloses(int year) =>
        SpecialEarlyCloses.Where(h => h.Date.Year == year);
}
