# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the **MarketData.Primitives** module. It provides foundational market-data domain types (bars, candles, quotes, resolutions) and market-hours services (NYSE calendar, session windows, holiday logic) used across the trading platform.

## Build Commands

```bash
# Build all projects
dotnet build MarketData.Primitives.sln

# Release build
dotnet build MarketData.Primitives.sln -c Release

# Run all tests
dotnet test tests/MarketData.Primitives.Tests/MarketData.Primitives.Tests.csproj

# Run a single test by name
dotnet test tests/MarketData.Primitives.Tests/ --filter "FullyQualifiedName~MethodName_Scenario"
```

Configurations: `Debug`, `Release`, `DebugIsolated`. GitVersion auto-generates `GitVersionInformation.g.cs` at build time.

## Architecture

Two-layer separation — do not collapse layers:

```
MarketData.Primitives    ← Domain: value objects, enums, collections (depends only on Core)
MarketData.Application    ← Contracts + implementations: service interfaces, domain models,
                            NYSE calendar/session logic, JSON holiday config
MarketData.Workers        ← Reusable worker-host infrastructure: market-aware scheduling, Hangfire
                            wiring, job dispatch/eventing/heartbeats, options, time-series doc base
                            (depends on Application; namespace MarketData.Workers)
MarketData.ServiceWorkers ← Host template: bundles job implementations + config on top of
                            MarketData.Workers (the only project a new worker clone re-creates)
```

> The former `MarketData.Infrastructure` project was retired and the calendar/session
> implementations (`NyseMarketCalendar`, `MarketContextProvider`, `MarketTimeZoneProvider`)
> live in `MarketData.Application` under the `MarketData.Application.Calendar` namespace.

### MarketData.Workers (reusable host infrastructure)

Class library (`src/MarketData.Workers`, namespace `MarketData.Workers`) holding everything a
service-worker host needs that does **not** vary per project. It uses a `Microsoft.AspNetCore.App`
framework reference so the hosting/DI/options extensions and Hangfire APIs resolve cleanly.

- **Dispatch (`Execution/`):** `Core.BackgroundJobExecutor` resolves/runs jobs by `Key`;
  `JobDispatcher` (the Hangfire target) wraps it to publish start/finish events, capture a
  `JobResult`, and record runs in `JobRunRegistry`. (`IBackgroundJob`/`JobExecutionContext`/
  `BackgroundJobExecutor` live in **`Core`** — namespace `Core` — not in Primitives/Application.)
- **Scheduling (`Scheduling/`):** hosted `MarketScheduler : IMarketScheduler` computes next-fire
  instants from `MarketClock`/`IMarketContextProvider` (reused from Application) and enqueues into
  Hangfire. Triggers (`ScheduleTrigger`): `IntervalAlways`, `MarketOpen`, `MarketClose`,
  `EveryNMinutesDuringMarketHours` (the "5m candle"), `Cron`. Waits use `TimeProvider`, so schedules
  are backtest-drivable under `Core.ManualTimeProvider`.
- **Hangfire** (in-memory storage, `Hosting/HangfireSetup`) is the durable executor + dashboard;
  eventing via `Eventing/IEventPublisher` + `SerilogEventPublisher` emitting `Core.BaseEvent`
  (service name in `Context`); `Heartbeat/HeartbeatService` publishes a periodic job-activity summary.
- **Persistence:** `Documents/TimeSeriesDocument` (`Core.IDocument` base) for concrete time-series docs.
- **Composition seam:** generic infra wires through
  `Hosting/ServiceWorkerHostExtensions.AddServiceWorkerCore` (knows no specific jobs); hosts register
  their jobs via `JobRegistrationExtensions.AddBackgroundJob<TJob>`.

### MarketData.ServiceWorkers (host template)

A runnable `Microsoft.NET.Sdk.Web` host (`src/MarketData.ServiceWorkers`) intended to be cloned into
a project template. It contains **only** what varies per project: `Program.cs` + `appsettings*.json`
(composition root) and `Jobs/` (namespace `MarketData.ServiceWorkers.Jobs`). All boilerplate comes
from the referenced `MarketData.Workers` library.

- Each host registers its jobs in `Jobs/JobRegistration.AddWorkerJobs` (typed HTTP clients via
  `IHttpClientFactory` + `Microsoft.Extensions.Http.Resilience`, document stores, options).
  `Program.cs` calls `AddServiceWorkerCore(config).AddWorkerJobs(config)`, maps the Hangfire
  dashboard at `/hangfire`, and exposes a dev-only `POST /run/{jobKey}` to enqueue on demand.
- Sample jobs: `HelloWorldJob` (interval) and `ExchangeRatesJob` (Treasury API → file DB).
  **Everything under `Jobs/` is a placeholder** — replace per host.

**External dependencies:** see `DEPENDENCIES.md` for published dependency paths and upstream usage notes. In particular, `Core.dll` is resolved from `$(BlueSkiesOutput)` and its published `README.md` should be read before making Core-dependent changes.

### Key Domain Types

| Type | Purpose |
|------|---------|
| `Bar` | OHLC + Volume with computed properties (Range, Body, BodyPercent, Score, IsBullish) |
| `Candle(Resolution, Timestamp)` | Time-stamped Bar with resolution awareness |
| `Resolution(uint Count, ResolutionUnit Unit)` | Flexible timeframe (Seconds → Years); variable-length units use calendar math |
| `CandleSeries` | Thread-safe, `ArrayPool<T>`-backed collection with lazy-cached price arrays |
| `Quote` | Bid/Ask/Last with spread calculations |
| `RatioSymbol` | Readonly struct representing a synthetic `"NUM/DEN"` symbol; parse/validate ratio notation |
| `RatioMath` | Pure static helpers to combine two candles or candle series into a ratio series |

### Key Application Contracts (`MarketData.Application.Contracts` / `.Services`)

The market-hours/session stack is a **synchronous, deterministic** engine (2.0). It is split on the
clock: a pure calendar plus a clock-aware context provider. The old async `IMarketTimingService` /
`MarketSession` / `MarketHoursStatus` / `MarketHoursProvider` / `NyseMarketHoursService` were removed.

- **`System.TimeProvider`** — primary clock abstraction (Core 2.0 ecosystem standard); business logic depends on this instead of `DateTime.UtcNow`. `GetUtcNow()` for the instant; `TimeProvider.System` live, `Core.ManualTimeProvider` (`Advance`/`SetUtcNow`) for simulation/backtest.
- **`IMarketCalendar`** — pure, **clock-free** date logic: `ClassifyDay`, `GetSessionWindow`, `GetCalendarYear`, `IsTradingDay`, `NextTradingDay`/`AddTradingDays`, `CountTradingDays`, `IsoWeekNumber`, `GetTradingDayOrdinal`, `GetPeriodStats`, `GetMonthlyExpiration`/`GetQuarterlyExpiration`/`GetWitchingDates`/`NextExpirationOnOrAfter`, `SettlementDate`.
- **`IMarketContextProvider`** — `TimeProvider`-aware: `GetContext()`/`GetContextAt(instant)`, `GetActivePhase`, `GetLiquidity`, `IsRegularSessionOpen`, `NextRegularOpenUtc`. Venue-agnostic (wraps any `IMarketCalendar`).
- **`MarketClock`** (`.Services`) — synchronous convenience facade (replaces `MarketHoursProvider`); **no sync-over-async**.
- **`MarketContext`** — single-call snapshot: active phase, `SessionLiquidityLevel`, regular open/elapsed/remaining/progress, per-phase `PhaseStatus` (overnight/pre/post), next transition, trading-day ordinals, period-end flags, next expirations, owning `TradingDate`.
- Records: **`MarketDayInfo`** (kind + holiday name), **`MarketSessionWindow`** (phase instants in UTC), **`Holiday`** (named, full vs early-close), **`MarketHolidayCalendarYear`**, **`PhaseStatus`**, **`TradingPeriodStats`**/**`TradingDayOrdinal`**, **`OptionsExpiration`**/`WitchingKind`.
- Enums (in `MarketData.Primitives.Sessions`): `SessionLiquidityLevel`, `MarketPhase`, `MarketDayKind`; value objects `PhaseWindow`, `VenueSchedule`.

### Calendar Implementations (`MarketData.Application.Calendar`)

- `NyseMarketCalendar` — `IMarketCalendar` for the composite US-equity venue (`"US-EQ"`). Holiday data composed in precedence order: computed rules (`UsEquityHolidayRules`, Computus Good Friday + observed/nth-weekday, incl. Juneteenth from 2022) → bundled named special closures (`UsEquityClosures`: mourning days, Hurricane Sandy, 9/11) → per-year JSON overrides (`HolidayOverrideLoader`). Per-year cache.
- **Phase model** (composite, ET): overnight futures (Sun 18:00 → Fri, daily 17:00–18:00 halt) → pre-market 04:00–09:30 → regular 09:30–16:00 (half-day 13:00) → post-market 16:00–20:00 (omitted on half days). Phases half-open `[open, close)`; higher-liquidity phase wins at shared boundaries. `VenueSchedules.UsEquityComposite()` is the schedule; add venues via new `VenueSchedule` + rules.
- Holiday JSON overrides: `~/OneDrive/TradingSystem/config/holidays/holidays-{year}.json`. New schema `{ "holidays":[{date,name}], "earlyCloses":[{date,name}] }`; legacy bare-date arrays (`holidays`/`halfDays`) still accepted (names synthesized).
- **Known limitations**: overnight uses the NYSE (not CME) calendar; bundled closures are historical-only (future ad-hoc closures via JSON); all quarterly expirations are `QuadWitching`.
- `MarketTimeZoneProvider` — `internal`; `For(timeZoneId)` resolves Windows/IANA ids with fallback (cached).

### Ratio Symbol Conventions

- `RatioSymbol` is the canonical way to represent and validate `"NUM/DEN"` synthetic symbols.
- Both legs are uppercased and trimmed on construction; nested slashes and empty halves are rejected.
- `RatioSymbol.IsRatio(string)` is the cheap check; `TryParse` / `Parse` produce a typed value.
- `RatioMath.CombineCandles` asserts matching `Resolution` and `Timestamp` — alignment is the **caller's** responsibility.
- High = `numHigh / denLow`, Low = `numLow / denHigh` — this preserves "high ≥ low" for the derived ratio.
- `Volume` of a ratio candle is always `0`; volume of a derived instrument is undefined.
- `CombineSeries` inner-joins on `Timestamp`, silently drops unpaired bars and bars where any denominator OHLC field is zero.

## Key Patterns and Constraints

- **`DateTimeOffset` over `DateTime`** throughout. `DateOnly`/`TimeOnly` for date/time-only semantics. UTC canonical; convert to local only at boundaries.
- **No `DateTime.UtcNow` in business logic** — always inject `System.TimeProvider` and call `GetUtcNow()`.
- **Venue is construction-time identity**, not a per-call parameter: build a calendar/provider for a venue (exposed as `VenueId`). Keep venue-specific assumptions out of shared logic so new venues are a `VenueSchedule` + holiday-rules swap.
- **`CandleSeries` performance** — uses `ArrayPool<T>` and lazy-cached `ReadOnlySpan<T>` price arrays; mutations invalidate the cache.
- **Value objects** inherit from `Core.ValueObject`; prefer `init`-only or private setters.
- **Test doubles** — use `Core.ManualTimeProvider` for the clock; prefix any other fake/stub implementations with `Fake`.

## Test Conventions

- xUnit with `Fact` and `Theory`/`InlineData`/`MemberData`
- Naming: `MethodName_Scenario_ExpectedResult`
- Cover timezone boundaries, DST transitions, and holiday/half-day edge cases for any market-hours work
