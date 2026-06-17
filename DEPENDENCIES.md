# External Dependencies

Published-artifact and feed notes for dependencies that are **not** restored from nuget.org.

## NuGet feed (`$(BlueSkiesOutput)`)

Internal packages are restored from the local/UNC feed `\\BART\MyNuget` (registered NuGet source
`Bart-MyNuget`; the same path is exposed via the `BlueSkiesOutput` environment variable).

| Package | Version | Used by | Notes |
|---------|---------|---------|-------|
| `Core` | 2.0.0 | all projects | Shared base library. Read its published `README.md`/`AGENTS.md` before Core-dependent changes. |
| `Core.Persistence` | 2.0.0 | `MarketData.Workers`, `MarketData.ServiceWorkers` | `IDocumentStore<T>` implementations (`JsonDocumentStore`, `MongoDocumentStore`) + `AddPersistence`/`AddDocumentStore<T>`. References `MongoDB.Driver`. |

### Core type locations (read before using)

`Core` owns several abstractions consumed here that are easy to misattribute:

- **`IDocument`, `IDocumentStore<T>`, `DocumentKey`** — persistence contracts (impls in `Core.Persistence`).
- **`BaseEvent`, `EventStatus`** — event record + status (`Unread`/`Read`/`Processing`/`Completed`).
  `BaseEvent` has no `Source` member; the worker host carries the service name in `Context`.
- **`IBackgroundJob`, `JobExecutionContext`, `BackgroundJobExecutor`** — background-job dispatch
  primitive (namespace `Core`). **Not** in the Core README/AGENTS as of 2.0.0.
- **`ManualTimeProvider`** — controllable `System.TimeProvider` for simulation/backtest/tests.
- **`Core.Json.CoreJson`** — canonical frozen `JsonSerializerOptions` (use for all wire/storage JSON).

> ⚠️ Same-version republish caveat: `Core` is currently stamped `2.0.0`. If the package is rebuilt
> and re-pushed without a version bump, consumers must purge the cached copy
> (`rm -rf ~/.nuget/packages/core/2.0.0` + `dotnet nuget locals http-cache --clear`) before the new
> content is picked up. Prefer bumping the version for non-trivial changes.

## Public packages (nuget.org)

Restored normally; listed here only where a version/behaviour note matters.

| Package | Version | Notes |
|---------|---------|-------|
| `Hangfire.AspNetCore` / `Hangfire.InMemory` | 1.8.23 / 1.0.0 | Job server + dashboard (in `MarketData.Workers`); in-memory storage. Pulls a transitive `Newtonsoft.Json` that triggers an `NU1903` audit warning — bump/override if warnings-as-errors is enabled. |
| `Microsoft.Extensions.Http.Resilience` | 10.1.0 | Polly v8 standard resilience handler for typed HTTP clients (used by host job registrations). |
| `Serilog.AspNetCore` (+ sinks/formatting) | 10.0.0 | Structured/semantic logging in the host, configured from `appsettings.json`. |

`MarketData.Workers` uses a `<FrameworkReference Include="Microsoft.AspNetCore.App" />` so all the
hosting/DI/options extensions and Hangfire.AspNetCore APIs resolve without per-package references.

## Holiday override config (runtime data, not a package)

`NyseMarketCalendar` reads per-year JSON overrides from
`~/OneDrive/TradingSystem/config/holidays/holidays-{year}.json` (see `CLAUDE.md`).
