namespace MarketData.Application.Contracts;

/// <summary>
/// Unified service for market timing, hours, and calendar information.
/// Consolidates functionality from both hours and calendar services.
/// Supports configurable holidays and half-days via JSON configuration files.
/// </summary>
public interface IMarketTimingService
{
    /// <summary>
    /// Determines whether a given date is a trading day (not a weekend, holiday, or full closure).
    /// </summary>
    /// <param name="venue">The trading venue (e.g., "NYSE").</param>
    /// <param name="date">The date to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the date is a trading day, false otherwise.</returns>
    Task<bool> IsTradingDayAsync(string venue, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Gets the market session hours for a given date.
    /// </summary>
    /// <param name="venue">The trading venue (e.g., "NYSE").</param>
    /// <param name="date">The date to get session for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="MarketSession"/> with open/close times, or null if not a trading day.</returns>
    Task<MarketSession?> GetSessionAsync(string venue, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Gets the list of holiday dates (full closures) for a given year.
    /// </summary>
    /// <param name="venue">The trading venue (e.g., "NYSE").</param>
    /// <param name="year">The year for which to retrieve holidays.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of holiday dates when the market is fully closed.</returns>
    Task<IReadOnlyList<DateOnly>> GetHolidaysAsync(string venue, int year, CancellationToken ct = default);

    /// <summary>
    /// Determines whether the market is currently open.
    /// </summary>
    /// <param name="venue">The trading venue (e.g., "NYSE").</param>
    /// <param name="asOfUtc">The point in time to check (in UTC).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the market is open at the specified time, false otherwise.</returns>
    Task<bool> IsOpenAsync(string venue, DateTimeOffset asOfUtc, CancellationToken ct = default);

    /// <summary>
    /// Gets the current market status (trading day, open status, session hours).
    /// </summary>
    /// <param name="venue">The trading venue (e.g., "NYSE").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="MarketHoursStatus"/> with current market information.</returns>
    Task<MarketHoursStatus> GetCurrentStatusAsync(string venue, CancellationToken ct = default);

    /// <summary>
    /// Gets today's regular-session close time expressed as UTC.
    /// </summary>
    /// <param name="venue">The trading venue (e.g., "NYSE").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The close time in UTC, or null when today is not a trading day (weekend or holiday).</returns>
    Task<DateTimeOffset?> GetTodayCloseUtcAsync(string venue, CancellationToken ct = default);
}
