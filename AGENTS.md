# AGENTS.md

## Purpose of this repository
`MarketData.Primitives` is the domain primitives library for market-data concepts:
- bars/candles/series
- resolution and time-bucketing logic
- market session helper primitives
- shared interfaces such as `ITimeKeeper`

Keep this project focused on deterministic, reusable domain types and logic.

## Architecture direction (agreed)
For new functionality related to market schedules/calendars/providers:

1. Add an **application layer project** to this solution.
   - Suggested name: `MarketData.Application` (or `MarketData.Application.Abstractions` if split).
2. Define service contracts in the application layer.
   - Examples: `IMarketCalendarService`, `IMarketHoursService`.
3. Keep external provider implementations in infrastructure.
   - Suggested future project: `MarketData.Infrastructure`.
4. Keep composition/DI registration in a host/API/worker project.

Do **not** couple provider-specific APIs or HTTP clients directly into primitives.

## Time model requirements
All calendar/session logic must support both:
- **real-time operation**
- **simulation/backtest operation**

Use `ITimeKeeper` as the primary clock abstraction for this behavior.
- Services that depend on "current time" should accept an `ITimeKeeper` (or an adapter built on it).
- Avoid directly calling `DateTime.UtcNow` / `DateTimeOffset.UtcNow` in business logic.
- Prefer APIs that are deterministic for a supplied time/date, plus explicit "now" helpers powered by `ITimeKeeper`.

## Contract design guidance
When introducing calendar/hour services:
- Prefer explicit venue/exchange identifiers in APIs.
- Model trading-day status and session windows (including half-days).
- Keep timezone behavior explicit.
- Separate pure calculations from I/O calls.
- Make simulation behavior reproducible by driving it from `ITimeKeeper`.

## Testing guidance
- Unit tests should cover:
  - timezone boundaries
  - DST transitions
  - holiday and half-day behavior
  - deterministic `ITimeKeeper`-driven scenarios
- Add tests for both real-time clock adapters and simulated/backtest clocks where relevant.

## Implementation guardrails for contributors
- Favor small, composable interfaces.
- Keep primitives serializable and side-effect free when possible.
- Avoid introducing infrastructure dependencies into `src/MarketData.Primitives`.
- If adding new projects, update solution structure and this document.
