# Copilot Instructions — MarketData.Primitives

## Repository purpose

This repo provides **foundational market-data domain types and market-hours services** for a trading system.
It is structured as two layers (no mixing allowed):

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `src/MarketData.Primitives` | Value objects: `Bar`, `Candle`, `CandleSeries`, `Resolution`, `Quote`, enums |
| Application | `src/MarketData.Application` | Contracts (`IMarketCalendar`, `IMarketContextProvider`, `MarketClock`, records; clock is the BCL `System.TimeProvider`) **and** their implementations (`NyseMarketCalendar`, `MarketContextProvider`, `MarketTimeZoneProvider`) |

> A separate `MarketData.Infrastructure` project was retired; its implementations now live in
> `src/MarketData.Application/Calendar/` under the `MarketData.Application.Calendar` namespace.

Tests live in `tests/MarketData.Primitives.Tests` and reference both layers.

An external git submodule at `src/external/Core` provides the base `ValueObject` class used by domain types.

---

## First steps after clone

```bash
# Initialize the Core submodule (required before any build or test)
git submodule update --init --recursive
```

---

## Build commands

> **Important**: The solution file (`.sln`) includes a reference to `../Core/Core/Core.csproj` that only resolves in the parent TradingSystem monorepo. In this standalone repo, always build **individual project files** or the test project, not the `.sln`.
>
> Also note: `MarketData.Primitives.csproj` bundles `GitVersion.MsBuild`. In CI / cloud-agent environments pass `/p:UseGitVersionTask=false` to prevent version-computation errors.

```bash
# Build all layers (recommended in standalone mode)
dotnet build tests/MarketData.Primitives.Tests/MarketData.Primitives.Tests.csproj /p:UseGitVersionTask=false

# Build individual projects
dotnet build src/MarketData.Application/MarketData.Application.csproj
dotnet build src/MarketData.Primitives/MarketData.Primitives.csproj /p:UseGitVersionTask=false
```

---

## Test commands

```bash
# Run all tests
dotnet test tests/MarketData.Primitives.Tests/MarketData.Primitives.Tests.csproj /p:UseGitVersionTask=false

# Run a single test by name
dotnet test tests/MarketData.Primitives.Tests/MarketData.Primitives.Tests.csproj /p:UseGitVersionTask=false \
  --filter "FullyQualifiedName~MethodName_Scenario"
```

All tests must remain green. Test framework is **xUnit** (no NUnit/MSTest).

---

## Architecture constraints

1. **Do not** add I/O or external-transport code to `MarketData.Primitives`.
2. **Do not** couple `MarketData.Primitives` to `MarketData.Application` (primitives has no project reference to application).
3. **Do not** call `DateTime.UtcNow` / `DateTimeOffset.UtcNow` directly in business logic — always inject `System.TimeProvider` and call `GetUtcNow()`.
4. **Do not** hard-code venue names — accept them as `string` parameters (e.g., `"NYSE"`).
5. Keep `IMarketCalendar` / `IMarketContextProvider` and their implementations in the **Application** layer: contracts in `Contracts/`, providers in `Calendar/`. The clock is the BCL `System.TimeProvider` — no custom clock abstraction to place.
6. All new public service contracts go in `src/MarketData.Application/Contracts/`; provider implementations go in `src/MarketData.Application/Calendar/`.
7. If adding a new project, update the solution file **and** `AGENTS.md`.

---

## Key types and where to find them

### Domain (`src/MarketData.Primitives/`)
- **`Bar`** — OHLC + Volume value object; inherits `Core.ValueObject`. Computed properties: `Range`, `Body`, `BodyPercent`, `Score`, `IsBullish`, `IsDoji`.
- **`Candle`** — Extends `Bar` with `Resolution` and `DateTimeOffset Timestamp`; provides `EndTime`.
- **`CandleSeries`** — `IDisposable`, `ArrayPool<Candle>`-backed, thread-safe collection. Has lazy-cached price arrays (`ClosePrices`, `OpenPrices`, …). Call `Dispose()` when done. Mutations invalidate the price-array cache.
- **`Resolution`** — `struct(uint Count, ResolutionUnit Unit)`. Supports `Seconds`→`Years`. Variable-length units (`Months`, `Quarters`, `Years`) require calendar math and **cannot** be converted to a fixed `TimeSpan` via `GetTimeSpan()`. Use `GetDurationToNextResolutionEvent(DateTimeOffset)` instead. `Parse("5m")` / `TryParse` supported.
- **`Quote`** — Bid/Ask/Last with computed `Spread` and `SpreadPercent`.

### Clock (BCL `System.TimeProvider`)
- **`TimeProvider`** — primary clock abstraction; `GetUtcNow()` for the instant. `TimeProvider.System` (production) or `Core.ManualTimeProvider` (`Advance`/`SetUtcNow`, deterministic) for sim/tests. Injected into `MarketContextProvider` / `MarketClock`.

### Application contracts (`src/MarketData.Application/Contracts/`)
- **`IMarketCalendar`** — pure, **clock-free**, synchronous calendar: `ClassifyDay`, `GetSessionWindow`, `GetCalendarYear`, `IsTradingDay`, `NextTradingDay`/`AddTradingDays`, `CountTradingDays`, `IsoWeekNumber`, `GetTradingDayOrdinal`, `GetPeriodStats`, `GetMonthlyExpiration`/`GetQuarterlyExpiration`/`GetWitchingDates`/`NextExpirationOnOrAfter`, `SettlementDate`.
- **`IMarketContextProvider`** — clock-aware: `GetContext`/`GetContextAt`, `GetActivePhase`, `GetLiquidity`, `IsRegularSessionOpen`, `NextRegularOpenUtc`, `CurrentOrNextRegularCloseUtc`.
- Records: **`MarketContext`** (single-call snapshot), **`MarketDayInfo`**, **`MarketSessionWindow`**, **`Holiday`**/`MarketHolidayCalendarYear`, **`PhaseStatus`**, **`TradingPeriodStats`**/`TradingDayOrdinal`, **`OptionsExpiration`**/`WitchingKind`. Enums + value objects (`SessionLiquidityLevel`, `MarketPhase`, `MarketDayKind`, `PhaseWindow`, `VenueSchedule`) live in `MarketData.Primitives.Sessions`.

### Application services (`src/MarketData.Application/Services/`)
- **`MarketClock`** — synchronous façade over `IMarketContextProvider` (`IsMarketOpen`, `Liquidity`, `Context`, `NextMarketOpen`, `TodayCloseUtc`, `Classify`, `TradingDaysUntil`). No sync-over-async.

### Calendar implementations (`src/MarketData.Application/Calendar/`)
- **`NyseMarketCalendar`** — `IMarketCalendar` for the composite US-equity venue (`"US-EQ"`). Holiday data composed: `UsEquityHolidayRules` (Computus Good Friday + observed/nth-weekday) → `UsEquityClosures` (bundled named specials) → `HolidayOverrideLoader` (per-year JSON). Per-year cache.
- **`MarketContextProvider`** — `IMarketContextProvider`; ctor `(IMarketCalendar, TimeProvider)`. Resolves instants → `MarketPhase`/`SessionLiquidityLevel`; DST-correct per-boundary conversion.
- **`VenueSchedules`** — schedule factory (`UsEquityComposite()`). **`MarketTimeZoneProvider`** — `internal static`; `For(timeZoneId)` resolves Windows/IANA ids with fallback (cached).

---

## Coding conventions

- **`DateTimeOffset` over `DateTime`** everywhere in business logic. Use `DateOnly`/`TimeOnly` for date/time-only values.
- UTC is canonical; convert to local only at display/boundary.
- Value objects inherit from `Core.ValueObject` and override `GetEqualityComponents()`.
- Use `init`-only properties or private setters on value objects.
- Prefer `record` for immutable data transfer objects (`MarketContext`, `MarketDayInfo`, `MarketSessionWindow`).
- No `static DateTime.UtcNow` calls — always inject `System.TimeProvider` and call `GetUtcNow()`.
- Venue is construction-time identity (`VenueId` on the calendar/provider), not a per-call parameter; keep venue-specific assumptions out of shared logic.

---

## Test conventions

- Framework: **xUnit** with `[Fact]` and `[Theory]`/`[InlineData]`/`[MemberData]`.
- Naming: `MethodName_Scenario_ExpectedResult`.
- Test doubles: prefix fake/stub implementations with `Fake`. Place them as `private sealed class` within the test class unless shared.
- Always cover: timezone boundaries, DST transitions, holiday/half-day edge cases for any market-hours code.
- For clock-driven tests, inject a `Core.ManualTimeProvider` — construct it with a fixed start instant and `Advance`/`SetUtcNow` to drive the timeline deterministically.

Example clock pattern (see `MarketClockTests.cs` / `MarketContextProviderTests.cs`):
```csharp
var clock = new ManualTimeProvider(new DateTimeOffset(2025, 5, 19, 20, 30, 0, TimeSpan.Zero)); // 16:30 ET
var provider = new MarketContextProvider(new NyseMarketCalendar(), clock);
var ctx = provider.GetContext();
```

---

## External dependency (git submodule)

`src/external/Core` → `https://github.com/csdelan/Core`

Provides: `Core.ValueObject` (structural-equality base class used by `Bar` and `Resolution`).

The `MarketData.Primitives.csproj` uses a conditional `<ProjectReference>`:
- Monorepo path: `../../../Core/Core/Core.csproj`
- Standalone path: `../external/Core/Core/Core.csproj`

Always run `git submodule update --init --recursive` before building.

---

## Versioning

`MarketData.Primitives.csproj` uses `GitVersion.MsBuild` (v6.5.0). In CI/cloud-agent environments, bypass it with `/p:UseGitVersionTask=false` to avoid build failures when git history is shallow or branch detection fails.

---

## Holiday configuration (runtime)

`HolidayOverrideLoader` looks for per-year JSON files at:
```
~/OneDrive/TradingSystem/config/holidays/holidays-{year}.json
```
Schema (named entries):
```json
{
  "holidays":    [ { "date": "2025-07-06", "name": "Special Closure" } ],
  "earlyCloses": [ { "date": "2025-07-03", "name": "Early Close" } ]
}
```
Legacy bare-date arrays (`"holidays": ["2025-01-01"]`, `"halfDays": [...]`) are still accepted; names are synthesized. Overrides take precedence over computed NYSE defaults (MLK, Washington's Birthday, Good Friday, Memorial Day, Juneteenth (from 2022), Independence Day, Labor Day, Thanksgiving, Christmas — with observed-holiday shifting) and the bundled special-closure table (`UsEquityClosures`).
