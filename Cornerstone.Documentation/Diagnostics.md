# Diagnostics (developer monitoring)

**Status:** Sample reworked onto AppDispatcher model projection (2026-08-11)  
**Date:** 2026-08-11

Opt-in developer surfaces for monitoring Cornerstone runtime: Keystone bus, AppDispatcher, and Profiler. Product analytics (e.g. Grok usage) are separate.

---

## Design rule

Diagnostics is **not** a second UI loop. It uses the same path as any feature:

```
Runtime / capture (worker)  →  diagnostics models (pending)
        →  DiagnosticsTabViewModel (DispatchableViewModel)
        →  AppDispatcher ApplyModelChanges
        →  thin View bindings
```

No `DispatcherTimer` for refresh or load. Open the panel → register capture; closed → null capture (zero cost).

---

## Goals

| Goal | Approach |
|------|----------|
| Zero cost when off | `DiagnosticsCapture == null`; bus history off unless `IsHistoryEnabled` |
| Session-level first | Bus ring, dispatcher snapshot, profiler scopes, optional feature load simulation |
| Models → VMs | `DiagnosticsSession` models + `TrackBinding` / `TrackCollection` / `TrackSeries` |
| Capture on dispatch | Optional `IDiagnosticsCapture` each poll; diagnostics dispatchable applied **last** |
| Bounded retention | Bus + model lists limited |

Deep timing (per-dispatchable apply ms, per-handler bus) remains out of v1.

---

## Types

| Type | Assembly | Role |
|------|----------|------|
| `IDiagnosticsCapture` | Cornerstone | Hook: `Capture(host, pendingApplyCount)` |
| `DiagnosticsSession` | Cornerstone | Models + capture implementation |
| `LoadSimulationDispatchable` | Cornerstone | Tracked feature root for synthetic ViewModel load |
| `ApplicationViewModel.DiagnosticsCapture` | Cornerstone | Register session while panel open |
| `ApplicationViewModel.DiagnosticsDispatchable` | Cornerstone | Applied once after the feature loop (not in Track membership) |
| `DiagnosticsTabViewModel` | Sample | Projects session models |
| `TabDiagnostics` | Sample | Attach/detach + commands only |

---

## AppDispatcher integration

Each worker `Update`:

1. Collect pending attached **feature** roots (`Track` set only).  
2. If `DiagnosticsCapture != null`, call `Capture` (writes models, may mark pending).  
3. If no feature pending and diagnostics does not need apply → idle return.  
4. UI-thread: apply each feature root (`SystemProfiler` apply count); then apply `DiagnosticsDispatchable` once if attached and dirty (**not** counted in apply-rate / batch size).  
5. Worker “applied” for Active/Idle is **feature-only**. Diagnostics-only apply must not force Active — otherwise mode capture (Idle↔Active) dirties the session every throttle cycle and the mode toggles forever.

While the panel is open, capture runs every idle/active poll. Models mark pending only when values change. Diagnostics is never in `_dispatchables` — no list ordering.

**Simulate ViewModel load** tracks `LoadSimulationDispatchable` as a real feature root. Each capture re-dirties it and calls `RequestDispatch`, so Active mode, **AppDispatcher.Apply / s**, batch size, and tracked list all reflect genuine feature apply traffic (not diagnostics-only apply).

---

## Bus message history

- `KeystoneBus.IsHistoryEnabled` (default false).  
- `History` ring after handlers complete (duration, handler count, error); capacity via `History.Limit`.  
- **Live record filter:** `KeystoneBus.HistoryFilter` — text grammar applied before `History.Add` (dropped events are gone).  
- **View filter:** session `ViewHistoryFilter` — same grammar over retained rows for UI search (does not delete the bus ring).  
- Session mirrors matching sequences into a `SpeedyList` model → `TrackCollection` to presentation list.

### Filter grammar (text bar)

Whitespace-separated tokens are **AND**. Empty = match all.

| Token | Meaning |
|-------|---------|
| `channel:Notification` | `ChannelName` contains value (case-insensitive) |
| `type:0` | message type int equals |
| `type:0,2` | type is 0 **or** 2 |
| `error:true` / `error:false` | `HadError` |
| free text (e.g. `ShowMessage`) | substring on Name, ErrorMessage, or ChannelName |

No channel/type dropdowns — hosts define channels; users copy names/types from the list into the bar.

---

## Sample usage

1. Open **Diagnostics**.  
2. **Simulate ViewModel load** — dirties a tracked feature root every poll; expect Mode Active, Apply chart ~active rate, Feature applies climbing.  
3. **Pulse load** — one-shot feature dirty.  
4. **Record history** + publish — bus list projects through the same apply path.

---

## Phases

| Phase | Status |
|-------|--------|
| Bus history instrumentation | Done |
| Model + capture + Sample VM | Done (this rework) |
| Keystone inspector + Editor panel | Later |
| Deep timing | Later |
