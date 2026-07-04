# External Dependencies

Published-artifact and feed notes for dependencies that are **not** restored from nuget.org.

## NuGet feed (`$(BlueSkiesOutput)`)

Internal packages are restored from the local/UNC feed `\\BART\MyNuget` (registered NuGet source
`Bart-MyNuget`; the same path is exposed via the `BlueSkiesOutput` environment variable).

| Package | Version | Used by | Notes |
|---------|---------|---------|-------|
| `Core` | 2.0.0 (Primitives, Application); 2.1.0 (WorkersCore, MarketWorkers) | all projects | Shared base library. WorkersCore/MarketWorkers pin the newer 2.1.0 because the `IBackgroundJob`/`JobExecutionContext`/`BackgroundJobExecutor` dispatch primitives were added after 2.0.0 (see note below). Read its published `README.md`/`AGENTS.md` before Core-dependent changes. |
| `Core.Persistence` | 2.0.0 | `MarketData.WorkersCore`, `MarketData.MarketWorkers` | `IDocumentStore<T>` implementations (`JsonDocumentStore`, `MongoDocumentStore`) + `AddPersistence`/`AddDocumentStore<T>`. References `MongoDB.Driver`. |

### Core type locations (read before using)

`Core` owns several abstractions consumed here that are easy to misattribute:

- **`IDocument`, `IDocumentStore<T>`, `DocumentKey`** — persistence contracts (impls in `Core.Persistence`).
- **`BaseEvent`, `EventStatus`** — event record + status (`Unread`/`Read`/`Processing`/`Completed`).
  `BaseEvent` has no `Source` member; the worker host carries the service name in `Context`.
- **`IBackgroundJob`, `JobExecutionContext`, `BackgroundJobExecutor`** — background-job dispatch
  primitive (namespace `Core`), available from `Core` 2.1.0 (**not** present in the 2.0.0 used by
  Primitives/Application) — this is why `MarketData.WorkersCore`/`MarketData.MarketWorkers` pin
  `Core` to 2.1.0. Not documented in the Core README/AGENTS as of 2.0.0.
- **`ManualTimeProvider`** — controllable `System.TimeProvider` for simulation/backtest/tests.
- **`Core.Json.CoreJson`** — canonical frozen `JsonSerializerOptions` (use for all wire/storage JSON).

> ⚠️ Same-version republish caveat: consumers pin specific `Core` versions (2.0.0 / 2.1.0, see
> above). If a pinned version is rebuilt and re-pushed without a version bump, consumers must purge
> the cached copy (`rm -rf ~/.nuget/packages/core/<version>` + `dotnet nuget locals http-cache --clear`)
> before the new content is picked up. Prefer bumping the version for non-trivial changes.

## Public packages (nuget.org)

Restored normally; listed here only where a version/behaviour note matters.

| Package | Version | Notes |
|---------|---------|-------|
| `Hangfire.AspNetCore` / `Hangfire.Storage.SQLite` | 1.8.23 / 0.4.1 | Job server + dashboard (in `MarketData.WorkersCore`); persistent SQLite storage shared by every worker process on a machine (path via `HangfireOptions.DbPath`, default `%ProgramData%\MarketData\hangfire.db`). Pulls a transitive `Newtonsoft.Json` that triggers an `NU1903` audit warning — bump/override if warnings-as-errors is enabled. |
| `Microsoft.Extensions.Http.Resilience` | 10.1.0 | Polly v8 standard resilience handler for typed HTTP clients (used by host job registrations). |
| `Serilog.AspNetCore` (+ sinks/formatting) | 10.0.0 | Structured/semantic logging in the host, configured from `appsettings.json`. |

`MarketData.WorkersCore` uses a `<FrameworkReference Include="Microsoft.AspNetCore.App" />` so all the
hosting/DI/options extensions and Hangfire.AspNetCore APIs resolve without per-package references.

## Holiday override config (runtime data, not a package)

`NyseMarketCalendar` reads per-year JSON overrides from
`~/OneDrive/TradingSystem/config/holidays/holidays-{year}.json` (see `CLAUDE.md`).
