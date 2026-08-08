# Logging

Cornerstone has two separate systems under `Cornerstone.Logging`:

| System | Purpose |
|--------|---------|
| **Logger** | Basic, in-memory operational log (circular buffer). |
| **Tracker** | Advanced structured path / exception analytics with repositories. |

They do **not** share models or call into each other. Use **Logger** for “what happened” operational messages; use **Tracker** when you need structured session paths stored or transmitted.

---

## Logger (memory-only circular buffer)

### Design

- Fixed capacity ring of preallocated entry shells (default **4096**, power of two).
- When full, **oldest entries are dropped**; `DroppedCount` increases.
- **No ETW / EventSource** — works the same on all TFMs (desktop, mobile, browser).
- **No static `Instance`** — construct or resolve via DI.
- Steady-state writes reuse entry shells (no `new` per log line for the entry itself). Message strings still come from the caller.

### Dependency injection

```csharp
[DependencyInjected] // singleton factory registration via source gen
public class Logger
{
    [DependencyInjectionConstructor]
    public Logger(IDateTimeProvider dateTimeProvider = null);

    public Logger(int capacity, IDateTimeProvider dateTimeProvider = null,
        LogLevel minimumLevel = LogLevel.Trace);
}
```

Resolve one shared `Logger` from `DependencyProvider` (or `new Logger(...)` in tests). Inject it into types that need to log (e.g. `SyncManager`, `SyncSession`, sync clients).

### Levels

```text
Trace < Debug < Information < Warning < Error < Critical
None  // MinimumLevel = None disables all writes
```

`IsEnabled(level)` / `MinimumLevel` gate the hot path before any write work.

### Writing

```csharp
logger.Write(LogLevel.Information, "Started");
logger.Write(LogLevel.Debug, sessionId, "Sync step");
logger.Write(LogLevel.Error, sessionId, "Failed", exception);
logger.Write(LogLevel.Information, sessionId, "At time", utcTimestamp);

logger.Information("…");
logger.Warning("…");
logger.Error("…", ex);
```

Optional `sessionId` is for correlation (e.g. sync). Use `Guid.Empty` when not applicable.

### Reading history

```csharp
LogEntryView[] entries = logger.Snapshot(); // oldest → newest
// each view: Sequence, Timestamp, Level, SessionId, Message, Exception
```

Snapshots **copy** field values. Do not assume live ring slots; after wrap, slots are reused.

### Tests / reset

```csharp
logger.Clear(); // empty ring, keep capacity, reset sequence/dropped
```

### Sync integration

Sync types take an optional `Logger` and call it when present. If no logger is supplied, log calls are no-ops. Prefer injecting the same app-wide singleton so Sync history appears in one place.

---

## Tracker (unchanged)

`Tracker` remains a separate, more advanced system for path/exception analytics (`TrackerPath`, repositories, sessions). Its APIs and storage model are independent of `Logger`. Do not route Tracker through Logger or vice versa.

---

## What is intentionally not included

- ETW / EventPipe exporters  
- File or console sink pipelines  
- Microsoft.Extensions.Logging provider host  
- Structured property bags / scopes  
- Zero-allocation message formatting  

Those can be added later as optional consumers of `Snapshot()` or as separate features without changing the core ring.