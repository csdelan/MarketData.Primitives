# USAGE.md

## Purpose

The `MarketData` projects provide three layers:

- `MarketData.Primitives`: core market-data models such as candles, quotes, resolutions, and time-series containers.
- `MarketData.Application`: business-facing contracts for market hours and time control.
- `MarketData.Infrastructure`: concrete implementations for real-time clock access and NYSE calendar/session behavior.

Use the primitives project to model price data. Use the application project to code against stable abstractions. Use the infrastructure project when you need a working implementation for real-time execution.

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

### `ITimeKeeper`

Use `ITimeKeeper` anywhere business logic needs the current time. This is the primary abstraction that keeps code deterministic and simulation-friendly.

Main members:

- `Now`: current effective time
- `SetTime(...)`: move time in a simulated implementation
- `WaitTime(...)`: await a target time

Use this instead of directly calling system time in strategy or market-hours logic.

### `IMarketTimingService`

Use `IMarketTimingService` as the main contract for venue-aware market calendar and hours logic.

Main capabilities:

- check whether a date is a trading day
- get a session for a specific date
- get a venue's holiday list
- check whether the market is open at a given UTC time
- get the current status for a venue
- get today's close time in UTC

This is the contract most higher-level services should depend on when they need exchange hours or holiday awareness.

### `MarketSession`

Use `MarketSession` to represent the open and close time for one trading date.

Main fields:

- `Open`
- `Close`
- `IsHalfDay`

This is the cleanest way to pass around one session window after calendar logic has been resolved.

### `MarketHoursStatus`

Use `MarketHoursStatus` when you need a snapshot of the current market state for a venue.

It includes:

- whether today is a trading day
- whether the market is open
- the local as-of time used for the evaluation
- the current session, if one exists

This is useful for dashboards, readiness checks, and scheduler-style logic.

### `MarketHoursProvider`

Use `MarketHoursProvider` as a synchronous facade over the market timing service when the calling code is still sync-oriented.

It is most useful for:

- legacy code paths
- simple utilities that need a yes/no market-open result
- getting the next open without directly handling async calls

Prefer `IMarketTimingService` directly in new async-first code.

## MarketData.Infrastructure

### `RealTimeTimeKeeper`

Use `RealTimeTimeKeeper` when you want `ITimeKeeper` backed by the live system clock.

Behavior:

- `Now` returns the current UTC time
- `SetTime(...)` is not supported
- `WaitTime(...)` delays until the requested time

Use this in production or live-monitoring workflows. Use a fake or simulated implementation elsewhere when you need deterministic tests or replayable scenarios.

### `NyseMarketHoursService`

Use `NyseMarketHoursService` when you need a working implementation of `IMarketTimingService` for the NYSE.

It handles:

- weekends
- standard NYSE holidays
- configured holiday overrides
- half-days
- regular session hours
- current open/closed status

This is the default concrete choice when your code needs real NYSE calendar behavior.

Important notes:

- venue support is currently NYSE-focused
- session times are resolved in Eastern Time
- `GetTodayCloseUtcAsync()` is useful when orchestration code needs a precise close timestamp in UTC

### `HolidayConfig`

Use `HolidayConfig` when you need to provide or persist yearly holiday and half-day overrides for the NYSE service.

It contains:

- `Holidays`: full market-closure dates
- `HalfDays`: shortened-session dates

This is mainly a configuration model rather than a day-to-day programming surface.

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
- `ITimeKeeper`

This combination fits live pricing and time-aware workflows that do not require bar consolidation.

### Exchange-hours-aware scheduling

Use:

- `IMarketTimingService`
- `MarketSession`
- `MarketHoursStatus`
- `RealTimeTimeKeeper`
- `NyseMarketHoursService`

This combination covers market-open checks, holiday-aware scheduling, and today's close-time calculations.

### Simulation or backtest-friendly logic

Use:

- `ITimeKeeper`
- `IMarketTimingService`

Write business logic against these abstractions so the same code can run against either a live clock or a simulated clock.
