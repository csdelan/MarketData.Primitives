# AGENTS.md

## Purpose of this repository
`MarketData.Primitives` solution contains layers for market-data/timekeeping work:
- `MarketData.Primitives` (domain primitives)
- `MarketData.Application` (service contracts/use-case-facing abstractions **and** their
  concrete provider implementations)
- `MarketData.WorkersCore` (reusable worker-host infrastructure: market-aware scheduling, Hangfire
  wiring, job dispatch/eventing/heartbeats, options, time-series document base; references
  `MarketData.Application`; assembly/namespace `MarketData.Workers`, pinned via `<AssemblyName>` so
  Hangfire's stored job records survive the project rename.)
- `MarketData.MarketWorkers` (host template: a runnable `Sdk.Web` host that bundles
  `Core.IBackgroundJob` jobs + config on top of `MarketData.WorkersCore`. Intended to be converted into a
  project template — see the "MarketData.WorkersCore" / "MarketData.MarketWorkers" sections of `CLAUDE.md`.)

Core primitives remain focused on deterministic, reusable domain types and logic. Reusable worker
infrastructure belongs in `MarketData.WorkersCore`; only job implementations + composition belong in
`MarketData.MarketWorkers` (or another host). Never push these runtime concerns into the lower layers.

> A separate `MarketData.Infrastructure` project was retired; the provider implementations
> (`NyseMarketCalendar`, `MarketContextProvider`, `MarketTimeZoneProvider`) live in
> `MarketData.Application` under the `MarketData.Application.Calendar` namespace.

## Architecture direction (agreed)
For market schedules/calendars/providers:

1. Define service contracts in the application layer (`MarketData.Application.Contracts`).
   - Example: `IMarketCalendar` (clock-free calendar) and `IMarketContextProvider` (clock-aware context).
2. Keep provider implementations in the application layer alongside the contracts
   (`MarketData.Application.Calendar`).
   - Exchange APIs, holiday feeds, cache, retries, persistence, system clock adapters.
   - If a future provider takes on heavy external/runtime dependencies (HTTP SDKs, persistence,
     etc.), reconsider extracting a dedicated infrastructure project at that time.
3. Keep composition/DI registration in a host/API/worker project.

Do **not** couple provider-specific APIs or HTTP clients directly into primitives.

## Time model requirements
All calendar/session logic must support both:
- **real-time operation**
- **simulation/backtest operation**

Use the BCL `System.TimeProvider` as the primary clock abstraction for this behavior (the
ecosystem standard adopted in Core 2.0).
- Services that depend on "current time" should accept a `TimeProvider` and call `GetUtcNow()`.
- Avoid directly calling `DateTime.UtcNow` / `DateTimeOffset.UtcNow` in business logic.
- Prefer APIs that are deterministic for a supplied time/date, plus explicit "now" helpers powered by `TimeProvider`.
- Production injects `TimeProvider.System`; simulation/backtest/tests inject `Core.ManualTimeProvider` (`Advance`/`SetUtcNow`).

## Contract design guidance
When introducing calendar/hour services:
- Prefer explicit venue/exchange identifiers in APIs.
- Model trading-day status and session windows (including half-days).
- Keep timezone behavior explicit.
- Separate pure calculations from I/O calls.
- Make simulation behavior reproducible by driving it from a `Core.ManualTimeProvider`.

## Provider placement rationale
- Keep **interfaces/contracts** in `MarketData.Application.Contracts` so use-cases depend on stable business-facing contracts.
- Keep **provider implementations** in `MarketData.Application.Calendar`. The current providers are
  pure calendar/timezone logic with no external transport, so a separate infrastructure project
  added overhead without benefit. Revisit that split only if a provider takes on external systems,
  process/runtime concerns, or environment wiring.

For this repository direction:
- `IMarketCalendar` / `IMarketContextProvider`: contracts and their concrete providers both in `MarketData.Application`.
- Clock: depend on the BCL `System.TimeProvider` directly — no MarketData-owned clock interface or implementation:
  - production wires `TimeProvider.System` at the composition root.
  - simulation/backtest/tests inject `Core.ManualTimeProvider`, still outside primitives business logic.

Why:
- It preserves deterministic business logic and testability.
- It avoids coupling primitives/application code to transport, OS clock access, or provider SDKs.
- It allows swapping providers per environment without changing domain/application code.

## Testing guidance
- Unit tests should cover:
  - timezone boundaries
  - DST transitions
  - holiday and half-day behavior
  - deterministic `ManualTimeProvider`-driven scenarios
- Add tests for both real-time clock adapters and simulated/backtest clocks where relevant.

## Implementation guardrails for contributors
- Favor small, composable interfaces.
- Keep primitives serializable and side-effect free when possible.
- Depend on `System.TimeProvider` for the clock; no MarketData-owned clock abstraction to place.
- Avoid introducing infrastructure dependencies into `src/MarketData.Primitives`.
- If adding new projects, update solution structure and this document.

## External dependency notes
- Dependency-specific published artifact guidance lives in `DEPENDENCIES.md`.
- Read `DEPENDENCIES.md` before making changes that depend on binaries resolved from `$(BlueSkiesOutput)`.

## Skills
A skill is a set of local instructions to follow that is stored in a `SKILL.md` file. Below is the list of skills that can be used. Each entry includes a name, description, and file path so you can open the source for full instructions when using a specific skill.
### Available skills
- skill-creator: Guide for creating effective skills. This skill should be used when users want to create a new skill (or update an existing skill) that extends Codex's capabilities with specialized knowledge, workflows, or tool integrations. (file: /opt/codex/skills/.system/skill-creator/SKILL.md)
- skill-installer: Install Codex skills into $CODEX_HOME/skills from a curated list or a GitHub repo path. Use when a user asks to list installable skills, install a curated skill, or install a skill from another repo (including private repos). (file: /opt/codex/skills/.system/skill-installer/SKILL.md)
### How to use skills
- Discovery: The list above is the skills available in this session (name + description + file path). Skill bodies live on disk at the listed paths.
- Trigger rules: If the user names a skill (with `$SkillName` or plain text) OR the task clearly matches a skill's description shown above, you must use that skill for that turn. Multiple mentions mean use them all. Do not carry skills across turns unless re-mentioned.
- Missing/blocked: If a named skill isn't in the list or the path can't be read, say so briefly and continue with the best fallback.
- How to use a skill (progressive disclosure):
  1) After deciding to use a skill, open its `SKILL.md`. Read only enough to follow the workflow.
  2) If `SKILL.md` points to extra folders such as `references/`, load only the specific files needed for the request; don't bulk-load everything.
  3) If `scripts/` exist, prefer running or patching them instead of retyping large code blocks.
  4) If `assets/` or templates exist, reuse them instead of recreating from scratch.
- Coordination and sequencing:
  - If multiple skills apply, choose the minimal set that covers the request and state the order you'll use them.
  - Announce which skill(s) you're using and why (one short line). If you skip an obvious skill, say why.
- Context hygiene:
  - Keep context small: summarize long sections instead of pasting them; only load extra files when needed.
  - Avoid deep reference-chasing: prefer opening only files directly linked from `SKILL.md` unless you're blocked.
  - When variants exist (frameworks, providers, domains), pick only the relevant reference file(s) and note that choice.
- Safety and fallback: If a skill can't be applied cleanly (missing files, unclear instructions), state the issue, pick the next-best approach, and continue.
