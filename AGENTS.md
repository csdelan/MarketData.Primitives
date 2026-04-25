# AGENTS.md

## Purpose of this repository
`MarketData.Primitives` solution now contains three layers for market-data/timekeeping work:
- `MarketData.Primitives` (domain primitives)
- `MarketData.Application` (service contracts/use-case-facing abstractions)
- `MarketData.Infrastructure` (provider implementations/runtime adapters)

Core primitives remain focused on deterministic, reusable domain types and logic.

## Architecture direction (agreed)
For market schedules/calendars/providers:

1. Define service contracts in the application layer.
   - Example: `IMarketTimingService` (consolidated market calendar and hours service).
2. Keep external provider implementations in infrastructure.
   - Exchange APIs, holiday feeds, cache, retries, persistence, system clock adapters.
3. Keep composition/DI registration in a host/API/worker project.

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

## Provider placement rationale (Application vs Infrastructure)
- Keep **interfaces/contracts** in the application layer (or application-abstractions split) so use-cases depend on stable business-facing contracts.
- Put **provider implementations** in infrastructure when they involve external systems, process/runtime concerns, or environment wiring.

For this repository direction:
- `IMarketTimingService`: consolidated contract in application; concrete providers in infrastructure.
- `ITimeKeeper`: keep abstraction in application contracts; implementations are infrastructure/runtime concerns:
  - real-time/system clock implementation belongs in infrastructure/runtime wiring.
  - simulation/backtest clock may live in test-support or simulation modules, but still outside primitives business logic.

Why:
- It preserves deterministic business logic and testability.
- It avoids coupling primitives/application code to transport, OS clock access, or provider SDKs.
- It allows swapping providers per environment without changing domain/application code.

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
- Keep `ITimeKeeper` in application contracts; implementations belong in infrastructure/test-support.
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
