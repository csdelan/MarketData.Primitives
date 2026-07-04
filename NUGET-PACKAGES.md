# NuGet Packages

Packages published from this repository to the local NuGet feed.

<!-- Managed by the nuget-publish skill. The publish script parses and updates the
     tables below - keep their column layout intact. -->

## Packages

| Package ID | Project | Current Version |
| --- | --- | --- |
| MarketData.WorkersCore | src/MarketData.WorkersCore/MarketData.WorkersCore.csproj | 2.3.0 |
| MarketData.Primitives | src/MarketData.Primitives/MarketData.Primitives.csproj | 2.3.0 |
| MarketData.Application | src/MarketData.Application/MarketData.Application.csproj | 2.3.0 |

## Version History

| Date | Package | Version | Notes |
| --- | --- | --- | --- |
| 2026-07-04 | MarketData.Application | 2.3.0 | Version-lockstep bump to keep pace with MarketData.Primitives 2.3.0 and MarketData.WorkersCore 2.3.0 (GitVersion global /p:Version override during a pack leaks into ProjectReference-derived dependency versions, so this chain moves together). |
| 2026-07-04 | MarketData.Primitives | 2.3.0 | Version-lockstep bump to keep pace with MarketData.WorkersCore 2.3.0 (GitVersion global /p:Version override leaked into this project dependency version during that pack). |
| 2026-07-04 | MarketData.WorkersCore | 2.3.0 | Hangfire SQLite queue poll interval now configurable via HangfireOptions.QueuePollIntervalMs (default 250ms, was the library default 15s) so manually-enqueued jobs are picked up quickly instead of averaging ~7.5s latency. |
| 2026-07-04 | MarketData.Application | 2.2.0 | Version-lockstep bump to keep pace with MarketData.Primitives 2.2.0 and MarketData.WorkersCore 2.2.0 (GitVersion's global /p:Version override during a pack leaks into ProjectReference-derived dependency versions, so this chain moves together). |
| 2026-07-04 | MarketData.Primitives | 2.2.0 | Version-lockstep bump to keep pace with MarketData.WorkersCore 2.2.0 (GitVersion's global /p:Version override during that pack leaked into this project's ProjectReference-derived dependency version, so the chain must move together). |
| 2026-07-04 | MarketData.WorkersCore | 2.2.0 | MarketScheduler.ComputeNextFire aligns IntervalAlways (and the default trigger case) to minute-boundary grid ceilings via GridCeiling, instead of firing IntervalMinutes after process start time. |
| 2026-07-04 | MarketData.Application | 2.1.0 | Version-lockstep bump to keep pace with MarketData.Primitives 2.1.0 and MarketData.WorkersCore 2.1.0 (GitVersion's global /p:Version override during a pack leaks into ProjectReference-derived dependency versions, so this chain moves together). |
| 2026-07-04 | MarketData.Primitives | 2.1.0 | Version-lockstep bump to keep pace with MarketData.WorkersCore 2.1.0 (GitVersion's global /p:Version override during that pack leaked into this project's ProjectReference-derived dependency version, so the chain must move together). |
| 2026-07-04 | MarketData.WorkersCore | 2.1.0 | Rebuild against current Core 2.1.0 (IBackgroundJob moved from Core.BackgroundJobs to Core); previous 2.0.0 package predated that move and was incompatible with Core 2.1.0 consumers. |
