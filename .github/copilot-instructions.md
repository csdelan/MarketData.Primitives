# Copilot Instructions — MarketData.Primitives

## Repository purpose

This repo provides **foundational market-data domain types and market-hours services** for a trading system.
It is structured as three layers (no mixing allowed):

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `src/MarketData.Primitives` | Value objects: `Bar`, `Candle`, `CandleSeries`, `Resolution`, `Quote`, enums |
| Application | `src/MarketData.Application` | Service contracts/interfaces: `ITimeKeeper`, `IMarketTimingService`, `MarketHoursProvider`, records |
| Infrastructure | `src/MarketData.Infrastructure` | Implementations: `NyseMarketHoursService`, `RealTimeTimeKeeper`, `MarketTimeZoneProvider` |

Tests live in `tests/MarketData.Primitives.Tests` and reference all three layers.

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
dotnet build src/MarketData.Infrastructure/MarketData.Infrastructure.csproj
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

1. **Do not** add infrastructure or I/O code to `MarketData.Primitives` or `MarketData.Application`.
2. **Do not** couple `MarketData.Primitives` to `MarketData.Application` (primitives has no project reference to application).
3. **Do not** call `DateTime.UtcNow` / `DateTimeOffset.UtcNow` directly in business logic — always inject `ITimeKeeper`.
4. **Do not** hard-code venue names — accept them as `string` parameters (e.g., `"NYSE"`).
5. Keep `ITimeKeeper` and `IMarketTimingService` in the **Application** layer; their implementations belong in **Infrastructure** or test-support.
6. All new public service contracts go in `src/MarketData.Application/Contracts/`.
7. If adding a new project, update the solution file **and** `AGENTS.md`.

---

## Key types and where to find them

### Domain (`src/MarketData.Primitives/`)
- **`Bar`** — OHLC + Volume value object; inherits `Core.ValueObject`. Computed properties: `Range`, `Body`, `BodyPercent`, `Score`, `IsBullish`, `IsDoji`.
- **`Candle`** — Extends `Bar` with `Resolution` and `DateTimeOffset Timestamp`; provides `EndTime`.
- **`CandleSeries`** — `IDisposable`, `ArrayPool<Candle>`-backed, thread-safe collection. Has lazy-cached price arrays (`ClosePrices`, `OpenPrices`, …). Call `Dispose()` when done. Mutations invalidate the price-array cache.
- **`Resolution`** — `struct(uint Count, ResolutionUnit Unit)`. Supports `Seconds`→`Years`. Variable-length units (`Months`, `Quarters`, `Years`) require calendar math and **cannot** be converted to a fixed `TimeSpan` via `GetTimeSpan()`. Use `GetDurationToNextResolutionEvent(DateTimeOffset)` instead. `Parse("5m")` / `TryParse` supported.
- **`Quote`** — Bid/Ask/Last with computed `Spread` and `SpreadPercent`.

### Application contracts (`src/MarketData.Application/Contracts/`)
- **`ITimeKeeper`** — `Now`, `SetTime`, `WaitTime`. Primary clock abstraction.
- **`IMarketTimingService`** — venue-aware async service: `IsTradingDayAsync`, `GetSessionAsync`, `GetHolidaysAsync`, `IsOpenAsync`, `GetCurrentStatusAsync`, `GetTodayCloseUtcAsync`.
- **`MarketSession`** — `record(TimeOnly Open, TimeOnly Close, bool IsHalfDay)`.
- **`MarketHoursStatus`** — `record(bool IsTradingDay, bool IsOpen, DateTimeOffset AsOfLocal, MarketSession? Session)`.

### Application services (`src/MarketData.Application/Services/`)
- **`MarketHoursProvider`** — synchronous façade over `IMarketTimingService` (uses `GetAwaiter().GetResult()`). Venue is hard-wired to `"NYSE"`.

### Infrastructure (`src/MarketData.Infrastructure/`)
- **`NyseMarketHoursService`** — `IMarketTimingService` for NYSE. Computus-based Easter/Good Friday. Holiday/half-day overrides loaded from JSON: `~/OneDrive/TradingSystem/config/holidays/holidays-{year}.json`. Falls back to built-in defaults if file missing. Throws `NotSupportedException` for non-NYSE venues.
- **`RealTimeTimeKeeper`** — Live `ITimeKeeper`; `SetTime` throws, `WaitTime` uses `Task.Delay`.
- **`MarketTimeZoneProvider`** — `internal static` helper; resolves Eastern Time zone cross-platform (`"Eastern Standard Time"` → `"America/New_York"` fallback).

---

## Coding conventions

- **`DateTimeOffset` over `DateTime`** everywhere in business logic. Use `DateOnly`/`TimeOnly` for date/time-only values.
- UTC is canonical; convert to local only at display/boundary.
- Value objects inherit from `Core.ValueObject` and override `GetEqualityComponents()`.
- Use `init`-only properties or private setters on value objects.
- Prefer `record` for immutable data transfer objects (`MarketSession`, `MarketHoursStatus`).
- No `static DateTime.UtcNow` calls — always inject `ITimeKeeper`.
- Venue identifier is always an explicit `string` parameter, never assumed or hard-coded in contracts.

---

## Test conventions

- Framework: **xUnit** with `[Fact]` and `[Theory]`/`[InlineData]`/`[MemberData]`.
- Naming: `MethodName_Scenario_ExpectedResult`.
- Test doubles: prefix fake/stub implementations with `Fake` (e.g., `FakeTimeKeeper`). Place them as `private sealed class` within the test class unless shared.
- Always cover: timezone boundaries, DST transitions, holiday/half-day edge cases for any market-hours code.
- For `ITimeKeeper`-driven tests, use a `FakeTimeKeeper` that accepts a `DateTimeOffset` in the constructor.

Example `FakeTimeKeeper` pattern (already in `InfrastructureServicesTests.cs`):
```csharp
private sealed class FakeTimeKeeper(DateTimeOffset now) : ITimeKeeper
{
    public DateTimeOffset Now { get; private set; } = now;
    public Task SetTime(DateTimeOffset time) { Now = time; return Task.CompletedTask; }
    public Task WaitTime(DateTimeOffset time) { Now = time; return Task.CompletedTask; }
}
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

`NyseMarketHoursService` looks for per-year JSON files at:
```
~/OneDrive/TradingSystem/config/holidays/holidays-{year}.json
```
Schema:
```json
{
  "holidays": ["2025-01-01", "2025-07-04"],
  "halfDays": ["2025-11-28", "2025-12-24"]
}
```
If the file is absent or unreadable, the service falls back to algorithmically computed NYSE defaults (MLK Day, Presidents' Day, Good Friday, Memorial Day, Independence Day, Labor Day, Thanksgiving, Christmas — with Saturday/Sunday observed-holiday shifting).
