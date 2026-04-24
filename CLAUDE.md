# CLAUDE.md

See also: [`DEPENDENCIES.md`](DEPENDENCIES.md) for published dependency guidance and external assembly notes.

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the **MarketData.Primitives** submodule of the TradingSystem monorepo. It provides foundational market-data domain types (bars, candles, quotes, resolutions) and market-hours services (NYSE calendar, session windows, holiday logic) used across the trading platform.

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

Three-layer separation — do not collapse layers:

```
MarketData.Primitives      ← Domain: value objects, enums, collections (depends only on Core)
MarketData.Application     ← Contracts: interfaces, domain models for services
MarketData.Infrastructure  ← Implementations: NYSE calendar, real-time clock, JSON config
```

**External dependencies:** see `DEPENDENCIES.md` for published dependency paths and upstream usage notes. In particular, `Core.dll` is resolved from `$(BlueSkiesOutput)` and its published `README.md` should be read before making Core-dependent changes.

### Key Domain Types

| Type | Purpose |
|------|---------|
| `Bar` | OHLC + Volume with computed properties (Range, Body, BodyPercent, Score, IsBullish) |
| `Candle(Resolution, Timestamp)` | Time-stamped Bar with resolution awareness |
| `Resolution(uint Count, ResolutionUnit Unit)` | Flexible timeframe (Seconds → Years); variable-length units use calendar math |
| `CandleSeries` | Thread-safe, `ArrayPool<T>`-backed collection with lazy-cached price arrays |
| `Quote` | Bid/Ask/Last with spread calculations |

### Key Application Contracts

- **`ITimeKeeper`** — primary clock abstraction; all business logic uses this instead of `DateTime.UtcNow`. Supports `SetTime`/`WaitTime` for simulation/backtest.
- **`IMarketTimingService`** — venue-aware market hours: `IsOpenAsync`, `GetSessionAsync`, `GetHolidaysAsync`, `GetTodayCloseUtcAsync`, `GetCurrentStatusAsync`.
- **`MarketHoursProvider`** — synchronous facade over `IMarketTimingService` (uses `GetAwaiter().GetResult()`).
- **`MarketSession`** / **`MarketHoursStatus`** — immutable records for session windows and current status.

### Infrastructure Details

- `NyseMarketHoursService` — NYSE calendar with Computus-based Easter/Good Friday, observed holidays, and half-days.
- Holiday/half-day overrides loaded from JSON: `~/OneDrive/TradingSystem/config/holidays/{year}.json`
- `RealTimeTimeKeeper` — live system clock implementation of `ITimeKeeper`
- `MarketTimeZoneProvider` — Eastern Time resolution utility

## Key Patterns and Constraints

- **`DateTimeOffset` over `DateTime`** throughout. `DateOnly`/`TimeOnly` for date/time-only semantics. UTC canonical; convert to local only at boundaries.
- **No `DateTime.UtcNow` in business logic** — always inject `ITimeKeeper`.
- **Venue as explicit string parameter** (e.g., `"NYSE"`) in all service APIs — do not hard-code assumptions.
- **`CandleSeries` performance** — uses `ArrayPool<T>` and lazy-cached `ReadOnlySpan<T>` price arrays; mutations invalidate the cache.
- **Value objects** inherit from `Core.ValueObject`; prefer `init`-only or private setters.
- **Test doubles** — `FakeTimeKeeper` in tests; prefix fake/stub implementations with `Fake`.

## Test Conventions

- xUnit with `Fact` and `Theory`/`InlineData`/`MemberData`
- Naming: `MethodName_Scenario_ExpectedResult`
- Cover timezone boundaries, DST transitions, and holiday/half-day edge cases for any market-hours work
