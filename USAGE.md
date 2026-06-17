# USAGE.md

## Purpose

The `MarketData` projects provide two layers:

- `MarketData.Primitives`: core market-data models such as candles, quotes, resolutions, and time-series containers.
- `MarketData.Application`: business-facing contracts for market hours and time control, **plus** the concrete NYSE calendar/session implementations that satisfy them.

Use the primitives project to model price data. Use the application project both to code against stable abstractions and to get a working implementation for real-time execution.

## MarketData.Primitives

### `Bar`

Use `Bar` when you need OHLCV data without attaching a timeframe or timestamp. It is the simplest price container in the library.

Main use cases:

- hold open, high, low, close, and volume values
- inspect candlestick shape through computed values such as `Range`, `Body`, `UpperWick`, `LowerWick`
- classify the bar quickly with `IsBullish`, `IsBearish`, `IsDoji()`, and `Score`

Choose `Bar` when you only care about the price shape itself and not when it happened.

### `Candle`

Use `Candle` when you need a `Bar` plus time context. A candle adds:

- `Timestamp`
- `Resolution`
- `EndTime`

This is the main type for historical bars, charting, and bar-based strategies. A candle represents a specific time bucket such as `1m`, `5m`, `1d`, or `1q`.

Typical usage:

- store historical bars returned from a data provider
- align calculations to bar timestamps and resolution boundaries
- determine when a bar window ends with `EndTime`

### `Resolution`

Use `Resolution` to represent a timeframe such as `1m`, `5m`, `1h`, `1d`, `1w`, `1mo`, `1q`, or `1y`.

It is central to all candle-based workflows:

- create a resolution directly from `Count` and `ResolutionUnit`
- parse shorthand values with `Resolution.Parse("5m")`
- convert back to shorthand with `ToShorthand()`
- find bucket boundaries with `GetLastEvent()` and `GetNextEvent()`

Important behavior:

- months, quarters, and years are variable-length units
- `GetTimeSpan()` works for fixed-length units and throws for variable-length units
- `GetDurationToNextResolutionEvent()` is the safe choice when you need the exact duration for calendar-based units

### `CandleSeries`

Use `CandleSeries` when you need an ordered collection of candles with fast repeated access. This is the primary container for chart windows, indicator input, and rolling analysis.

Main capabilities:

- append candles and maintain a consistent series
- expose candles as spans for efficient processing
- retrieve cached close, open, high, low, and volume arrays
- slice with `AsSpan(range)` or `TakeLast(count)`
- copy subranges with `CopyRange()`
- roll up the full series with `Consolidate()`

Typical usage:

- load a lookback window for a strategy
- pass close/high/low arrays into indicator calculations
- keep a rolling chart data set in memory

Operational note:

- `CandleSeries` implements `IDisposable`
- dispose it when you are done, especially in long-running or high-volume workflows

### `CandleSeriesExtensions`

Use these helpers for common series transformations:

- `Sorted()` to enforce chronological ordering
- `TrimToMax()` to keep only the most recent bars
- `FilterToSession()` to keep bars inside a session window
- `FilterToNyseRegularSession()` to keep only NYSE regular-hours bars
- `Aggregate()` to roll smaller bars into a larger resolution

This is the easiest way to clean, trim, session-filter, and consolidate candle data before analysis.

### `Quote`

Use `Quote` for point-in-time market quotes instead of completed bars. It is intended for bid/ask/last snapshots.

Main properties:

- `Bid`
- `Ask`
- `Last`
- `Timestamp`
- `Spread`
- `SpreadPercent`

Choose `Quote` for real-time pricing or spread-sensitive workflows, not for bar-history analysis.

### `ChartData`

Use `ChartData` as a simple wrapper when a chart or chart-like workflow needs to carry a candle series as one object.

This is useful when APIs or UI models want one chart payload instead of passing a raw `CandleSeries`.

### `BarMetrics`

Use `BarMetrics` to store computed analytics keyed by symbol, timeframe, and timestamp.

It is a good fit for:

- feature engineering output
- indicator snapshots
- metric versioning
- persisted derived values for a given bar

The `Values` dictionary lets you attach named decimal metrics without creating a new class for every metric set.

### `RatioSymbol`

Use `RatioSymbol` to parse, validate, and carry a synthetic ratio symbol of the form `"NUM/DEN"`.

Main use cases:

- validate that a user-supplied symbol string is a ratio before routing it through a decorator
- extract the numerator and denominator legs for separate data fetches
- produce a canonical string representation that can round-trip through parse

Key behaviors:

- both legs are uppercased and whitespace-trimmed on construction
- `RatioSymbol.IsRatio(string)` is the fast check; returns false for null, empty, missing slash, empty half, or multiple slashes
- `RatioSymbol.Parse(string)` throws `FormatException` on invalid input; `TryParse` is the non-throwing alternative
- `ToString()` always returns `"UPPER/UPPER"` regardless of the original casing

### `RatioMath`

Use `RatioMath` to combine two pre-fetched candle collections into a synthetic ratio series. This is a pure utility with no I/O — callers are responsible for fetching aligned data.

`CombineCandles(Candle numerator, Candle denominator)` produces one ratio candle:

- `Open = num.Open / den.Open`, `Close = num.Close / den.Close`
- `High = num.High / den.Low`, `Low = num.Low / den.High` — this preserves the invariant that high ≥ low for the derived ratio
- `Volume = 0` (volume of a ratio is undefined)
- throws `ArgumentException` if resolutions or timestamps differ
- throws `DivideByZeroException` if any denominator OHLC field is zero

`CombineSeries(IReadOnlyList<Candle> numerator, IReadOnlyList<Candle> denominator)` produces the full ratio series:

- inner-joins both legs on `Timestamp`; drops bars present on only one side
- silently skips bars where any denominator OHLC field is zero
- returns results sorted ascending by `Timestamp`

### `RthEstimator`

Use `RthEstimator` when you need a simple estimate of how many intraday bars fit into one NYSE regular trading day.

This is useful for rough sizing and planning, such as:

- expected bars per session
- chart window sizing
- default lookback counts for intraday resolutions

### `IContractDescriptor`, `SecurityType`, and `OptionType`

Use these types when a workflow needs lightweight contract metadata without depending on a larger securities model.

`IContractDescriptor` exposes:

- `Symbol`
- `UnderlyingSymbol`
- `SecurityType`
- `Multiplier`

`SecurityType` and `OptionType` provide the shared asset taxonomy used across workflows.

## MarketData.Application

### `TimeProvider` (clock abstraction)

Use the BCL `System.TimeProvider` anywhere business logic needs the current time. This is the
primary abstraction that keeps code deterministic and simulation-friendly, standardized across
the ecosystem by Core 2.0.

Main members:

- `GetUtcNow()`: current effective UTC instant
- `GetTimestamp()` / `GetElapsedTime(...)`: `Stopwatch`-style elapsed measurement
- `CreateTimer(...)` (or `Task.Delay(delay, timeProvider)`): await a target time

Use this instead of directly calling `DateTimeOffset.UtcNow` in strategy or market-hours logic.
Inject `TimeProvider.System` in production and `Core.ManualTimeProvider` in simulation/tests.

### `IMarketCalendar`

Use `IMarketCalendar` as the main contract for **clock-free** market calendar logic. Every method
takes explicit dates, so calendar math is fully deterministic and testable without a clock.

Main capabilities:

- classify a date (`ClassifyDay`) — regular day, half day, weekend, or named holiday
- resolve a date's session windows to UTC instants (`GetSessionWindow`)
- get a year's named holidays and early closes (`GetCalendarYear`)
- navigate/count trading days (`NextTradingDay`, `AddTradingDays`, `CountTradingDays`, `SettlementDate`)
- numbering and statistics (`IsoWeekNumber`, `GetTradingDayOrdinal`, `GetPeriodStats`)
- options expiration / witching dates (`GetMonthlyExpiration`, `GetQuarterlyExpiration`, `GetWitchingDates`, `NextExpirationOnOrAfter`)

### `IMarketContextProvider`

Use `IMarketContextProvider` for **clock-aware** queries. It wraps an `IMarketCalendar` plus an
injected `TimeProvider` and answers "what's happening now" questions:

- `GetContext()` / `GetContextAt(instant)` — a rich `MarketContext` snapshot
- `GetActivePhase` / `GetLiquidity` — the current `MarketPhase` and `SessionLiquidityLevel`
- `IsRegularSessionOpen`, `NextRegularOpenUtc`, `CurrentOrNextRegularCloseUtc`

### `MarketContext`

Use `MarketContext` to get everything about the current moment in one call: active phase and liquidity,
whether the regular session is open (with elapsed/remaining/progress), per-phase status for overnight
futures / pre-market / post-market, the next phase transition, trading-day ordinals, week/month/quarter
end flags, the owning trading date, and the next monthly/quarterly expirations.

### `MarketDayInfo`, `MarketSessionWindow`, `Holiday`

`MarketDayInfo` is the day classification (kind + holiday name + is-trading-day). `MarketSessionWindow`
holds a trading date's phase boundaries as UTC instants. `Holiday` is a named full-closure or early-close
entry; `MarketHolidayCalendarYear` groups a year's holidays and early closes.

### `MarketClock`

Use `MarketClock` as the synchronous convenience facade over `IMarketContextProvider`
(`IsMarketOpen`, `Liquidity`, `Context`, `NextMarketOpen`, `TodayCloseUtc`, `Classify`,
`TradingDaysUntil`). The whole stack is synchronous — there is no sync-over-async anywhere.

### Clock implementations

There is no MarketData-specific clock type anymore — use the standard `TimeProvider`
implementations directly:

- **Production / live monitoring:** `TimeProvider.System` (the live system clock; time cannot be
  set).
- **Tests / simulation / replay:** `Core.ManualTimeProvider` — a deterministic clock that only
  moves when you call `Advance(span)` or `SetUtcNow(instant)`. Timers created against it (and
  `Task.Delay(delay, provider)`) fire exactly when the clock is advanced across their due time,
  so a whole simulated timeline is single-stepped and reproducible. Construct with a start instant
  (`new ManualTimeProvider(startUtc)`) or the parameterless ctor (fixed epoch `2000-01-01Z`).

### `NyseMarketCalendar`

Use `NyseMarketCalendar` as the working `IMarketCalendar` implementation for the composite US-equity
venue (`"US-EQ"`). Construct it (optionally with a custom `VenueSchedule` and/or `HolidayOverrideLoader`)
and pass it to a `MarketContextProvider`.

It handles:

- weekends, observed holidays, half-days, and Computus-based Good Friday (`UsEquityHolidayRules`)
- bundled named special closures — mourning days, Hurricane Sandy, 9/11 (`UsEquityClosures`)
- per-year JSON holiday overrides (`HolidayOverrideLoader`)
- the full 24h phase model: overnight futures → pre-market → regular → post-market

Important notes:

- venue is construction-time identity (`VenueId`); add venues via a new `VenueSchedule` + rules
- all session times are resolved in Eastern Time, DST-correct per boundary
- overnight futures follow the NYSE (not CME) calendar — a documented simplification

### Holiday overrides (`HolidayOverrideLoader`)

Provide per-year corrections via JSON at `~/OneDrive/TradingSystem/config/holidays/holidays-{year}.json`.

New schema (named entries):

```json
{ "holidays": [ { "date": "2025-07-06", "name": "Special Closure" } ],
  "earlyCloses": [ { "date": "2025-07-03", "name": "Early Close" } ] }
```

Legacy bare-date arrays (`"holidays": ["2025-07-06"]`, `"halfDays": [...]`) are still accepted; names
are synthesized. Overrides take precedence over computed rules and the bundled closure table.

## Typical usage patterns

### Historical bar analysis

Use:

- `Candle`
- `Resolution`
- `CandleSeries`
- `CandleSeriesExtensions`

This combination covers loading bars, trimming a lookback, filtering to session hours, and aggregating into larger timeframes.

### Real-time quote handling

Use:

- `Quote`
- `TimeProvider`

This combination fits live pricing and time-aware workflows that do not require bar consolidation.

### Exchange-hours-aware scheduling

Use:

- `IMarketCalendar` / `NyseMarketCalendar`
- `IMarketContextProvider` / `MarketContextProvider`
- `MarketClock`
- `TimeProvider.System`

This combination covers market-open checks, holiday-aware scheduling, liquidity/phase awareness, and today's close-time calculations.

### Ratio symbol analysis

Use:

- `RatioSymbol`
- `RatioMath`
- `Candle`, `CandleSeries`

Typical flow: check `RatioSymbol.IsRatio(symbol)`, call `Parse` to get the typed value, fetch each leg independently, then pass both leg series to `RatioMath.CombineSeries`. Feed the resulting `IReadOnlyList<Candle>` into a `CandleSeries` or directly into indicator calculations — indicators consume OHLCV and ignore the symbol field entirely.

### Simulation or backtest-friendly logic

Use:

- `TimeProvider` (inject `Core.ManualTimeProvider` for the simulated clock)
- `IMarketCalendar` / `IMarketContextProvider`

Write business logic against these abstractions so the same code can run against either a live clock or a simulated clock.
