# MarketData.MarketWorkers

A runnable **service-worker host template** that bundles scheduled background jobs on top of the
reusable `MarketData.WorkersCore` library. Clone or adapt this project for each new worker
process — the only things that change per project are the files under `Jobs/` and the matching
entries in `appsettings.json`.

## Project structure

```
MarketData.MarketWorkers/
  Program.cs                   # Composition root — wires core + jobs, maps Hangfire dashboard
  appsettings.json             # All configuration (see sections below)
  appsettings.Development.json # Dev overrides (verbose logging)
  Jobs/
    JobRegistration.cs         # The one file you edit when adding/removing jobs
    HelloWorld/
      HelloWorldJob.cs         # Minimal sample: logs a line, no external deps
    Todo/
      TodoItem.cs              # IDocument — the stored entity
      TodoClient.cs            # Typed HttpClient for jsonplaceholder
      TodoJob.cs               # Fetches and saves a to-do item; demos HTTP + document store
```

Everything else — scheduling, Hangfire wiring, eventing, heartbeats, market-calendar integration,
persistence factory — comes from `MarketData.WorkersCore` and requires no changes.

## Running

```powershell
dotnet run --project src/MarketData.MarketWorkers
```

On startup you will see:

- Serilog console output showing the Hangfire server and each schedule's computed next-fire time.
- `hello-world` firing every 15 seconds (Cron trigger); `fetch-todo` firing every minute (IntervalAlways).
- `JobStarted` / `JobFinished` events in the log.
- A MeshTransit liveness heartbeat broadcast on `tcp://*:9101`, carrying status and a compact
  per-job activity digest (consumed by a central monitor via MeshTransit's `HeartbeatWatcher`).
- JSON files written to `data/todos/` after each `fetch-todo` run.

**Hangfire dashboard:** `http://localhost:5099/hangfire`

**Manual job trigger (dev):** `POST http://localhost:5099/run/{jobKey}`

## Configuration

### `ServiceWorker`

| Key | Default | Description |
|-----|---------|-------------|
| `ServiceName` | `MarketData.ServiceWorkers` | Stamped onto events and heartbeats |
| `VenueId` | `US-EQ` | Exchange calendar used for market-relative schedules |
| `DashboardPath` | `/hangfire` | URL path for the Hangfire UI |
| `ExposeHangfireDashboard` | `true` | Set `false` on worker-only instances sharing a central dashboard |
| `Heartbeat:EventEndpoint` | `tcp://*:9101` | PUB socket the MeshTransit liveness heartbeat binds (unique per process) |
| `Heartbeat:IntervalMs` | `5000` | Heartbeat cadence; death detected after ~`IntervalMs × missTolerance` |

### `Schedules`

```jsonc
"Schedules": {
  "Jobs": [
    { "JobKey": "hello-world", "Trigger": "Cron",          "Cron": "*/15 * * * * *", "Enabled": true },
    { "JobKey": "fetch-todo",  "Trigger": "IntervalAlways", "IntervalMinutes": 1,      "Enabled": true }
  ]
}
```

**Trigger types:**

| Trigger | Required fields | Fires |
|---------|----------------|-------|
| `Cron` | `Cron` (6-field, seconds supported) | On a fixed cron schedule via Hangfire recurring jobs |
| `IntervalAlways` | `IntervalMinutes` | Every N minutes, regardless of market hours |
| `MarketOpen` | — | At the next regular session open (09:30 ET) |
| `MarketClose` | — | At the next regular session close (16:00 ET, or 13:00 on half-days) |
| `EveryNMinutesDuringMarketHours` | `IntervalMinutes` | Every N minutes, only while the regular session is open; skips to next open otherwise |

### `Hangfire`

```jsonc
"Hangfire": {
  "DbPath": "C:\\ProgramData\\MarketData\\hangfire.db"
}
```

All worker processes on the same machine should point at the same `DbPath` so they share a single
job store and any one of them can serve the consolidated dashboard.

### `Persistence`

```jsonc
"Persistence": {
  "JsonRootPath": "data",
  "DefaultBackend": "Json",
  "Stores": {
    "Todo": "Json"
  }
}
```

See **Switching to MongoDB** below for the full set of options.

## Adding a new job

1. Create a folder under `Jobs/MyJob/` and add:
   - `MyJobDocument.cs` — a record implementing `IDocument` (or extending `TimeSeriesDocument`).
   - `MyJobClient.cs` — a typed `HttpClient` wrapper (if the job calls an external API).
   - `MyJob.cs` — implements `IBackgroundJob`; `Key` is a unique kebab-case string.

2. Register everything in `JobRegistration.cs`:

   ```csharp
   services.AddHttpClient<MyJobClient>(http =>
       http.BaseAddress = new Uri("https://example.com/"))
       .AddStandardResilienceHandler();

   services.AddDocumentStore<MyJobDocument>(
       storeName: "MyJob",
       collectionName: "my-job",
       jsonSubDirectory: "my-job");

   services.AddBackgroundJob<MyJob>();
   ```

3. Add a schedule entry to `appsettings.json`:

   ```jsonc
   { "JobKey": "my-job", "Trigger": "IntervalAlways", "IntervalMinutes": 5, "Enabled": true }
   ```

   The `JobKey` must match `MyJob.Key`.

## Switching to MongoDB

Document persistence is config-only — no code changes are required. `MongoDB.Driver` is already
a transitive dependency of `Core.Persistence`.

### Step 1 — Add the MongoDB connection block

In `appsettings.json`, add a `Mongo` object inside the `Persistence` section:

```jsonc
"Persistence": {
  "Mongo": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "MarketData"
  },
  "JsonRootPath": "data",
  "DefaultBackend": "Json",
  "Stores": {
    "Todo": "Json"
  }
}
```

### Step 2 — Switch stores to MongoDB

**Option A — Flip all stores at once** by changing `DefaultBackend`:

```jsonc
"Persistence": {
  "Mongo": { "ConnectionString": "mongodb://localhost:27017", "DatabaseName": "MarketData" },
  "JsonRootPath": "data",
  "DefaultBackend": "Mongo"
}
```

**Option B — Switch individual stores** while leaving others on JSON:

```jsonc
"Persistence": {
  "Mongo": { "ConnectionString": "mongodb://localhost:27017", "DatabaseName": "MarketData" },
  "JsonRootPath": "data",
  "DefaultBackend": "Json",
  "Stores": {
    "Todo": "Mongo"
  }
}
```

The `Stores` key matches the `storeName` argument used in `JobRegistration.cs`
(`AddDocumentStore<TodoItem>(storeName: "Todo", ...)`).

### Step 3 — Verify

Run the host and trigger `fetch-todo`:

```powershell
dotnet run --project src/MarketData.MarketWorkers
# In another terminal:
Invoke-RestMethod -Method Post http://localhost:5099/run/fetch-todo
```

Connect to MongoDB and confirm the document was written:

```javascript
use MarketData
db["todos"].find().pretty()
```

### Notes

- The MongoDB client connects lazily — if `DefaultBackend` stays `"Json"` and no store is set to
  `"Mongo"`, the MongoDB connection is never opened and a running `mongod` is not required.
- `MongoConventions` (registered automatically by `AddPersistence`) mirrors the `CoreJson` policy:
  enums as strings, `decimal` as `Decimal128`, offset-preserving `DateTimeOffset`. On-disk JSON
  and MongoDB documents have consistent representations.
- For production, prefer a connection string stored in user secrets or an environment variable
  rather than in `appsettings.json`:

  ```powershell
  dotnet user-secrets set "Persistence:Mongo:ConnectionString" "mongodb://..."
  ```

  The `UserSecretsId` (`marketdata-serviceworkers`) is already set in the project file.
