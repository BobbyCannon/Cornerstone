# AppDispatcher

AppDispatcher is an **optional auto layer** that keeps **ViewModels in sync with Keystone State** for **visual representation and user input** (what the user sees, and two-way fields they edit). It runs an **adaptive poll loop**: slow when quiet, faster while work is flowing. It only processes ViewModels that are **attached to a View**, so detached UI pays no apply cost.

It is **not** a second engine. Domain rules stay in Keystone (Bus · State · Processor) and run **off** the UI dispatcher. `ApplyModelChanges` / Track\* lambdas map and format for the view; they do not implement business logic. `RequestDispatch` only wakes the poll — it does not run Keystone on the UI thread.

Manual / custom UI integration remains fully valid without this layer — see [ViewIntegration.md](ViewIntegration.md).

**Exception:** `TextEditor` / `TextEditorViewModel` (and `Terminal`) hold most document state (buffer, caret, undo, tokens) in the ViewModel because of how the control is built. That is a **design limitation**, not a template for feature tabs. See [Agent/TextEditor.md](Agent/TextEditor.md).

---

## Layering

| Layer | Role |
|-------|------|
| **Keystone** | Application business logic only; off the UI dispatcher; never requires `IDispatcher.Dispatch` |
| **ViewIntegration** | How any UI can attach to Keystone State (manual wiring allowed) |
| **AppDispatcher** | Optional UI loop: project State → ViewModel for display / input (`HasModelChanges` / `ApplyModelChanges`) |
| **Dispatch bindings** | Optional *inside* a ViewModel: `Track*` methods that copy State or format the view on each apply tick |

---

## Goals

| Goal | How it is achieved |
|------|--------------------|
| Low idle CPU | Worker **parks** with real waits; **idle ~10 Hz** safety poll when quiet |
| Low latency when busy | **Active ~120 Hz** via `IntervalTimer` after apply or `RequestDispatch`; decay after empty ticks |
| Skip invisible UI | Multi-owner `Attach`/`Detach`; `IsAttached` when any owner remains |
| Cheap pending checks | `IDispatchPending.HasPending` / binding `HasPending()` — O(bindings), not O(list) |
| Coalesce high-rate text | `TextIngress` stages tokens; `TrackIngress` drains once per tick |
| Coalesce list membership | `SpeedyList` marks pending; `TrackCollection` reconciles into presentation lists |
| Keep models UI-free | Models mutate freely; ViewModels pull/apply on the dispatch thread |

---

## Adaptive worker (idle / active)

This is the core runtime model of `ApplicationViewModel` as `IAppDispatcher`.

### Why adaptive (hybrid wait)

A **fixed** always-on high-rate timer (e.g. 120 Hz `IntervalTimer` forever) is expensive when idle: short periods force `Sleep(0)` / spin for most of each interval.

| Strategy | Idle CPU | Active rate fidelity |
|----------|----------|----------------------|
| Fixed 120 Hz `IntervalTimer` always | High (~few %) | Excellent |
| Park wait only (even at 120 Hz) | Low if period is long; poor at short periods | `Thread.Sleep` / OS timer resolution cannot reliably hit 120 Hz |
| **Hybrid (this design)** | Low (park at ~10 Hz) | Excellent while active (`IntervalTimer` at default **120 Hz**) |

**Important distinction:** empty `Update()` is cheap. Idle multi-percent CPU was dominated by **always-on busy waits**, not by apply. Continuous high `AppDispatcher.Apply` rate means something is **actually dirty every tick**.

### Modes and wait primitives

| Mode | Default rate | Wait primitive | Enter when |
|------|--------------|----------------|------------|
| **Idle** | **10 / s** | `ManualResetEventSlim.Wait(timeout)` — real park | Lifecycle start, or after **N** consecutive ticks with no apply and no request |
| **Active** | **120 / s** | **`IntervalTimer.WaitForNextTickAsync`** — precise short periods | Apply ran, **or** idle wait ended because of `RequestDispatch` |

Default **N** (`idleTicksBeforeThrottle`) = **8** → about **~67 ms** of quiet active ticks at 120 Hz before returning to idle (timer disposed so idle stays cheap).

```
                    RequestDispatch()  ──────────────────────┐
                                                             ▼
  Idle (10 Hz, park)  ── apply OR request ──►  Active (120 Hz, IntervalTimer)
       ▲                                              │
       └── N empty ticks; dispose IntervalTimer ◄─────┘
```

Why two primitives:

- **Idle:** long timeout; OS sleep is fine; `RequestDispatch` unparks immediately.
- **Active:** only `IntervalTimer` can sustain true high rates (e.g. 120 Hz ≈ 8.3 ms). A plain `Wait(8 ms)` does not hit that rate reliably on Windows.

### Worker algorithm

On `StartLifecycle`:

1. Create `CancellationTokenSource` + `ManualResetEventSlim` (wake signal).
2. `Task.Run` the worker until cancel.

Each iteration:

1. **If active:** ensure `IntervalTimer(ActiveInterval)`; consume any set wake flags as `requested`; `await WaitForNextTickAsync(ct)`.
2. **If idle:** dispose any active timer; `wake.Wait(IdleInterval, ct)` (timeout or `RequestDispatch` / stop).
3. **`UpdateAsync`** — poll tracked roots; if any attached root has model changes, **await** `DispatchAsync` of `ApplyModelChanges` on the UI thread at `DispatcherPriority.Render` (does not `Invoke`/join the UI thread). Returns **true** if any feature apply ran.
4. **Advance mode** (`AdaptiveDispatchMode`):
   - `applied || requested` → active, streak = 0
   - else increment streak; if active and streak ≥ N → idle (dispose `IntervalTimer`)
5. Publish `IsDispatchActive` for diagnostics.

On `StopLifecycle`: cancel + set wake event + dispose active timer so neither wait hangs.

### Correctness vs latency

| Concern | Mechanism |
|---------|-----------|
| **Correctness** | Idle **and** active **poll** `IsAttached && HasModelChanges()`. Producers need not notify the dispatcher. Worst-case apply delay while idle ≈ one idle period (~100 ms). |
| **Latency** | `RequestDispatch()` unparks idle immediately and forces **active** mode so subsequent ticks use **`IntervalTimer`** at the active rate (default 120 Hz). |

`RequestDispatch` is **optional**. Missing it never drops work; it only delays ramp-up until the next idle tick or until a poll finds pending work (which itself enters active).

### `RequestDispatch`

```csharp
// IAppDispatcher — any thread, coalescing
void RequestDispatch();
```

| Property | Behavior |
|----------|----------|
| Thread-safe | Yes |
| Coalescing | Multiple sets before the next wait collapse to one wake |
| While idle | Unparks `ManualResetEventSlim` → next `Update` → enter active |
| While active | Flag consumed before each `IntervalTimer` tick → keeps active streak (even if that tick applies nothing) |
| Before `StartLifecycle` | No-op (wake event not created yet) — safe to call |
| After stop | Prefer not to call after teardown |

**When to call (optional, for snappier UI):**

- After staging high-rate text (`TextIngress.Append` batches)
- After bulk list mutations you care about showing immediately
- After custom `DispatchPending.MarkPending` for charts / series

**When you can skip:**

- Low-rate settings edits (idle poll is enough)
- Anything already dirty that will be seen within ~100 ms

There is **no** automatic wire from every `MarkPending` / list mutator in v1 (see gaps). Poll remains the safety net.

### Configuration

`ApplicationViewModel` constructor (also used by Agent / Sample / template `AppViewModel` via `base(dependencyProvider, dispatcher)`):

| Parameter | Default constant | Default value |
|-----------|------------------|---------------|
| `activeUpdatesPerSecond` | `DefaultActiveUpdatesPerSecond` | **120** (`IntervalTimer`) |
| `idleUpdatesPerSecond` | `DefaultIdleUpdatesPerSecond` | **10** (parked wait) |
| `idleTicksBeforeThrottle` | `DefaultIdleTicksBeforeThrottle` | **8** |

```csharp
// Defaults (recommended for most hosts)
: base(dependencyProvider, dispatcher)

// Custom: quieter idle, softer active
: base(dependencyProvider, dispatcher,
    activeUpdatesPerSecond: 60,
    idleUpdatesPerSecond: 5,
    idleTicksBeforeThrottle: 10)
```

Do **not** run `IntervalTimer` while idle. Active high rate is intentional and short-lived after quiet ticks.

### Diagnostics

| Signal | Meaning |
|--------|---------|
| `IsDispatchActive` | Worker is on the `IntervalTimer` active path |
| `ActiveInterval` / `IdleInterval` | Configured periods (public on `ApplicationViewModel`) |
| `LastApplyBatchSize` | How many roots applied on the last apply tick |
| `CopyTrackedDispatchables` | Snapshot of tracked roots for membership UI |
| `SystemProfiler` + `ApplyScopeName` (`"AppDispatcher.Apply"`) | Count/rate of UI applies (opt-in; null = zero cost) |
| Idle CPU still high with apply rate ~0 | Unexpected wake spam or other app work — not empty apply |
| Apply rate stuck near active rate | Something stays dirty every tick (`HasModelChanges` never clears) |

Developer panel design and bus history: [Diagnostics.md](Diagnostics.md).

Optional host hooks (null = off):

| API | Role |
|-----|------|
| `DiagnosticsCapture` | `IDiagnosticsCapture.Capture` each poll before UI apply |
| `DiagnosticsDispatchable` | Not Track()'d; applied once after the feature loop when attached and dirty |

### Unit tests

| Area | Location |
|------|----------|
| Mode transitions (no wall clock) | `Tests/.../AdaptiveDispatchModeTests.cs` (`AdaptiveDispatchMode`) |
| Wake + stop | `Tests/.../ApplicationViewModelDispatchTests.cs` |

---

## Foundation: `IDispatchPending`

Coarse **“view needs an update”** signal for the dispatch poll. **Not** a substitute for `ITrackPropertyChanges` (property-bit graphs / `ApplyChangesTo`).

```csharp
// Cornerstone/Presentation — IDispatchPending / DispatchPending
public interface IDispatchPending
{
    bool HasPending { get; }
    void ClearHasPending();
}
```

| Type | Pending set by | Clear behavior |
|------|----------------|----------------|
| **`DispatchPending`** | Explicit `MarkPending()` | Clears flag |
| **`SpeedyList<T>`** | Structural mutators (Add/Remove/Insert/indexer/Clear/…) | Clears flag only (list data kept) |
| **`TextIngress`** | Staged character count via `Append` | `ClearHasPending` is a **no-op**; consume with `Drain` / `DrainTo`, discard with `Clear()` |

Use `DispatchPending` for custom work (charts, derived fields):

```csharp
var pending = new DispatchPending();
// producer: pending.MarkPending();
// optional: appDispatcher.RequestDispatch();
TrackBinding(pending, () => viewSeries.CopyFrom(modelSeries));
```

**Relation to property change tracking**

| | `ITrackPropertyChanges` | `IDispatchPending` |
|--|-------------------------|--------------------|
| Granularity | Per-property bits | One coarse flag / buffered count |
| Apply model | `ApplyChangesTo` / `UpdateWith` | Caller projection (drain, reconcile, custom) |
| Clear API | `ResetHasChanges()` | `ClearHasPending()` |
| AppDispatcher wake | Idle poll only (unless you call `RequestDispatch`) | Same |

List pending covers **membership / order / slot replace**. Deep property edits on items without list mutation are a separate concern (`ReconcileListAndItems` + item `HasChanges`, or scalar model apply).

---

## Core types

### `IAppDispatcher`

```csharp
public interface IAppDispatcher
{
    Profiler SystemProfiler { get; set; }  // null = no system profiling cost
    void RequestDispatch();                // optional: wake / ramp to active
    void Track(DispatchableViewModel dispatchableViewModel);
    void Release(DispatchableViewModel dispatchableViewModel);
}
```

Implemented by `ApplicationViewModel` (e.g. Agent/Sample `AppViewModel`).

### System profiling (optional)

| Mechanism | Behavior |
|-----------|----------|
| `SystemProfiler == null` | Default. Apply path does no profiling work. |
| `SystemProfiler = profiler` | Opt in. Each UI `ApplyModelChanges` does `profiler.Increment("AppDispatcher.Apply")` (count/rate only; no clocks). |
| `Profiler.Increment` | Cheap rate samples; use when duration is irrelevant. Prefer over `Time(name, Action)` for always-on system rates. |
| `Profiler.Time` / `Start` | Duration + count; for cost analysis (render, pack, etc.). |

Constant: `ApplicationViewModel.ApplyScopeName` (`"AppDispatcher.Apply"`). Chart view updates/sec from that scope — not from property setters or per-binding timers.

Production apps leave `SystemProfiler` null unless diagnostics are enabled (DI assign or debug menu).

### `DispatchableViewModel` / `DispatchableViewModel<T>`

```
DispatchableViewModel : ViewModel
    ├── Attach(owner) / Detach(owner)   // multi-owner; cascade to children
    ├── IsAttached                      // any owner remains
    ├── TrackDispatchChild / ReleaseDispatchChild
    ├── HasModelChanges()               // virtual — pending bindings by default
    ├── ApplyModelChanges()             // virtual — apply pending bindings
    ├── TrackIngress(...)
    ├── TrackCollection(...)
    ├── TrackBinding(...)
    ├── TrackSeries(...)
    ├── TrackProperties(model) → IPropertyMap (MapOneWay / MapTwoWay)
    ├── TrackDerived(Action)   // format after the copies, no source of its own
    └── TrackIntent(name, publish)  // user gesture → bus; skipped while applying

DispatchableViewModel<T> : DispatchableViewModel
    where T : IUpdateable, ITrackPropertyChanges
    ├── Model / AutoUpdateModel
    ├── HasModelChanges()  → base || Model.HasChanges()
    └── ApplyModelChanges() → base then Model.ApplyChangesTo(this)
```

**`Attach` / `Detach` / `IsAttached`**

How views hook this automatically: [ViewIntegration.md](ViewIntegration.md#automatic-attach--detach-avalonia).

- **Owner is required** (not null): a **View** (`Attach(this)`) or a **parent** dispatchable cascading to children. There is no anonymous / null-owner attach.
- Idempotent per owner; `IsAttached` is true while **any** owner remains (not a single bool flip).
- Parent registers nested VMs with `TrackDispatchChild`; on 0→1 attach it calls `child.Attach(parent)`, on last detach `child.Detach(parent)`.
- Avalonia bases (`CornerstoneUserControl`, `Control`, `ContentControl`, `TemplatedControl`, `Window`, `AppView`) call `Attach(this)` / `Detach(this)` from **visual tree** attach/detach for **ViewModel and DataContext independently** (via `DispatchableVisualTree`). They never set or clear those properties for this purpose. First attach of a non-dispatchable owner also `IAppDispatcher.Track`s the VM into the apply set; last detach `Release`s it.
- AppDispatcher polls only **direct** apply-loop roots (`IsAttached` + `HasModelChanges`). Nested work flows down: each `ApplyModelChanges` applies itself then its **direct** `TrackDispatchChild` children (no grand-child collection).

### Bindings (`IDispatchBinding`)

Registered in the ViewModel constructor (or later). Owned by the VM; **not** registered with `IAppDispatcher`.

| API | Behavior |
|-----|----------|
| `TrackIngress(TextIngress, Action<ReadOnlySpan<char>>)` | Drain consumer when `HasPending` |
| `TrackIngress(TextIngress, IStringBuffer)` | `DrainTo` destination |
| `TrackCollection(source, dest, comparer?, mode?)` | Same-type list; source must be `IList<T>` **and** `IDispatchPending` |
| `TrackCollection(source, dest, same, create, update, remove)` | Model row → row ViewModel; `create` gets the source row; `remove` after a dest row is dropped; then `ClearHasPending` |
| `TrackBinding(IDispatchPending, Action)` | Custom apply then `ClearHasPending` |
| `TrackBinding(IDispatchBinding)` | Fully custom binding |
| `TrackSeries(model, view)` | Fixed-length series copy when versions differ |
| `TrackSeries(pending, getView, setView, buildSamples)` | Build samples from a pending source; then `ClearHasPending` |
| `TrackProperties(ITrackPropertyChanges)` | Property-to-property map (rename / convert / two-way); see below |
| `TrackDerived(Action)` | Presentation formulas after other bindings apply; see [Track\* methods](#track-methods) |
| `TrackIntent(propertyName, publish)` | User changed a view property → publish a bus message; skipped while applying; see [Track\* methods](#track-methods) |

### Property maps (`TrackProperties`)

Prefer a **shared contract** over a long chain of `MapOneWay` / `MapTwoWay` when the ViewModel and model already share names and types.

| Need | Prefer |
|------|--------|
| Full settings page, same names/types | `DispatchableViewModel<AppSettings>` + `AutoUpdateModel` |
| Large 1:1 slice (dashboard vs State) | Shared interface + `[ProjectFrom<TContract>]` on the VM (generated destination bag) + `TrackProperties<TContract>(model, this)` |
| Partial slice, rename, or type convert | Extra `.Map…` on that same map (or a small dedicated map) |
| Lists | `TrackCollection` (same type, or model → row factory) |
| High-rate text | `TrackIngress` |

**`TrackProperties<TContract>(model, this)`** walks public properties on `TContract` that exist on both sides:

- **Get-only** on the contract → **one-way** (model → view). Use this for display dashboards so the UI cannot write State.
- **Get/set** on the contract → **two-way** (same name, identity).

Leave lists, display-only strings, and converted fields **off** the contract. Chain a few `.MapOneWay` / `.MapTwoWay` only for those leftovers.

Do **not** hand-list twenty same-name `MapOneWay` calls. That is what the contract overload replaces.

```csharp
// Dashboard: IGrokHomeUsage is get-only → all those scalars one-way
TrackProperties<IGrokHomeUsage>(home, this)
    .MapOneWay<string, string>(
        nameof(GrokHomeUsageState.PeriodType),
        nameof(PeriodType),
        GrokUsageAnalytics.FormatPeriodTypeDisplay); // convert stays off the contract

// Two-way: settings path (string) ↔ VM selection (ModelInfo); names may differ
TrackProperties(state.Settings)
    .MapTwoWay(
        nameof(AppSettings.SelectedModel),
        nameof(SelectedModel),
        path => ResolveModel(path),          // string → ModelInfo
        model => model?.FilePath);           // ModelInfo → string
```

| API | Direction |
|-----|-----------|
| `MapOneWay(modelName, viewName, toView)` | Model → view only |
| `MapTwoWay(name)` | Both ways, same name, identity |
| `MapTwoWay(modelName, viewName)` | Both ways, rename, identity |
| `MapTwoWay(modelName, viewName, toView, toModel)` | Both ways, rename + convert |

**Behavior**

- Pending is based on **mapped property change bits** only (`ITrackPropertyChanges` / `ResetHasChanged` per property).
- First apply **seeds** inbound (current model values without requiring a prior dirty bit).
- Each tick: **outbound first** (user edit wins), then inbound.
- Inbound sets the view under a suppress flag and clears the view’s change bit so the same value is not written back (loop guard).
- Equality gates skip no-op writes.

Unit coverage: `Tests/Cornerstone.UnitTests/Presentation/DispatchableViewModelPropertyMapTests.cs`.

### Track* methods

Each `Track*` call registers an `IDispatchBinding` on the ViewModel. On a dispatcher tick the VM asks every binding “are you dirty?” and, if so, “apply.” Most bindings have a **source** that can answer dirty: property-change bits, `SpeedyList.HasPending`, `TextIngress`, or a `DispatchPending` flag.

| Call | Source of dirtiness | What it does |
|------|---------------------|--------------|
| `TrackProperties` | Mapped property bits on State / settings | Copy scalars onto the ViewModel (optional two-way) |
| `TrackCollection` | `SpeedyList.HasPending` | Reconcile a presentation list, then **clear** pending. Projected: `same`, `create`, `update`, `remove` (`create` gets the source row; `remove` after a dest row is dropped) |
| `TrackBinding` / derived `TrackSeries` | `IDispatchPending` | Custom copy (or chart samples), then **clear** pending |
| `TrackIngress` | Staged character count | Drain text once per tick |
| `TrackDerived` | **No source of its own** | Run **after** another binding on this VM actually applied |
| `TrackIntent` | **Not an apply binding** | On user `PropertyChanged`, publish a bus message. Does not run during apply |
| `ReleaseTracks` | **Not an apply binding** | Drops this VM's Track* recipe. `UninitializeLifecycle` already calls it. Rebind hosts call it before wiring a new session/repo |

`TrackIntent` is the user → domain half. `Track*` copies State onto the ViewModel. When the user edits a combo, slider, or other view property that means “run processor work,” register `TrackIntent(nameof(SelectedPeriod), () => bus.SelectPeriod(...))`. `ApplyModelChanges` (and `BeginProjecting`) set `IsProjecting` so those same assignments from `TrackDerived` / `TrackProperties` do not publish. Do not keep `_isProjecting*` flags on the feature VM.

Write-through settings still use `DispatchableViewModel<T>` + `AutoUpdateModel` (or `MapTwoWay`). Use `TrackIntent` when the gesture must publish, not write State.

`TrackDerived` is not another way to copy State. It rebuilds fields that are **formulas over values the other tracks just wrote**: status sentences, combo selection match, view-clock labels, tooltips, `HasAnalytics`. Those strings are not on State.

That is the same idea as:

```csharp
public override void ApplyModelChanges()
{
    base.ApplyModelChanges(); // all the copies
    ProjectDerived();         // then format
}
```

`TrackDerived(ProjectDerived)` **is** that override, registered as a binding so every feature uses one rule:

1. **First apply always runs** (seed) so an empty tab gets “Loading…” / “not found” instead of blanks.
2. **Later applies run only if some real `Track*` copied something this tick.**
3. Derived **never clears** State pending and **never reads `HasChanges()` on State**. Resetting those bits from the ViewModel would break persistence; polling them would pin the dispatcher at the active rate.

Register `TrackDerived` **last** so property / list / chart bindings apply first in the same tick. The apply loop skips derived bindings in the first pass, then runs them if `applied || NeedsSeed`.

**Coarse, not source-aware.** After seed, derived runs if **any** sibling binding on that ViewModel applied — including maps for properties the lambda does not read. It does **not** run when the whole VM is quiet. A tighter design would declare “only if `UsagePercent` / `Sessions` are dirty”; that is extra bookkeeping and is not implemented.

**Do not use `TrackSeries` twice on the same pending list.** The first apply clears `HasPending`, so a second series (or a caption) would miss the tick. One `TrackBinding` that fills every series and caption from that list is the right consumer. `TrackDerived` can then format flags that depend on those fills (`HasAnalytics` from day counts).

**Host lists (one tab per State row).** `ApplicationViewModel` is the dispatcher, not a `DispatchableViewModel`, so it cannot call `TrackCollection` itself. Attach a small child dispatchable to the host (`Attach(this)` — the host is `IAppDispatcher`) and put `TrackCollection` / `TrackDerived` there. `create` constructs the tab from the source row; `remove` `Release`s it. Seed with `ApplyModelChanges` after the first load so the strip is not empty until the next idle poll.

**Not a Track path:** View → domain (period pick, scrubber, refresh) publishes on the bus. Processors mutate State. Tracks only project inbound.

#### Workflow: usage dashboard

```csharp
TrackProperties<IGrokHomeUsage>(home, this);     // 1 copy scalars
TrackCollection(home.Sessions, Sessions, ...);   // 2 copy rows
TrackBinding(home.DailyTokenTotals, FillCharts); // 3 copy charts
TrackDerived(ProjectDerived);                    // 4 format UI from 1–3
TrackIntent(nameof(SelectedPeriod), SelectIt);   // 5 user combo → bus (not during 1–4)
```

**Tick 1 — seed (tab just attached).** Processor already filled `UsagePercent`, sessions, daily totals. `HasModelChanges()` is true (maps not seeded, lists pending, derived not seeded). Apply: map writes 62%, collection writes three rows, binding builds series, then `ProjectDerived` sets `SelectedPeriod`, `StatusText` (“Updated … · 3 session(s) · Live”), tooltips, view-clock text. Derived marks itself seeded.

**Tick 2 — nothing changed.** No bits, no list pending, derived is seeded. `HasModelChanges()` is false. Dispatcher parks.

**Tick 3 — `home.UsagePercent = 80`.** Property map applies; `applied = true`; derived runs and rebuilds pace / ETA / status from the new 80%. Charts and sessions are left alone.

**Tick 4 — a session is added.** Collection reconciles; derived runs so status can read `Sessions.Count` **after** the reconcile.

**Tick 5 — only the daily-total list changes.** `TrackBinding` fills series + captions; derived updates `HasAnalytics` from the day counts those fills just stored.

`ProjectDerived` may format and match combo items. It must not copy `UsagePercent` from State (`TrackProperties`), rebuild `Sessions` (`TrackCollection`), or publish `SelectPeriod` / `SetViewAsOf` (bus). Matching the combo by assigning `SelectedPeriod` is safe: apply is projecting, so `TrackIntent` does not publish.

Unit coverage: `DispatchableViewModelBindingTests.TrackIntentDoesNotPublishDuringApply`.

Tiny standalone example:

```csharp
TrackProperties(order);                    // OrderTotal, ItemCount
TrackCollection(order.Lines, Lines, ...);
TrackDerived(() =>
{
    SummaryText = ItemCount == 0
        ? "Empty cart"
        : $"{ItemCount} items · {OrderTotal:C}";
});
```

`SummaryText` is not on State. When `ItemCount` or the line list changes, those tracks apply, then this lambda runs.

Unit coverage: `DispatchableViewModelBindingTests.TrackDerivedSeedsThenReappliesWhenAnotherBindingApplies`.

**Collection modes** (`CollectionReconcileMode`)

- `List` — add/remove only (`ReconcileList`)
- `ListAndItems` — add/remove/update/order (`ReconcileListAndItems`; presentation overload when dest is `IPresentationList<T>`)

Core stays Avalonia-free: pass `Output.Append` for editors so document-change events fire (do not drain into the raw gap buffer).

### `ApplicationViewModel`

| Responsibility | Behavior |
|----------------|----------|
| Worker loop | Idle: park (`ManualResetEventSlim`); active: **`IntervalTimer`** |
| Tick rates | Idle **10**/s, active **120**/s, throttle after **8** empty ticks (ctor overrides) |
| `RequestDispatch` | Unpark idle / keep active streak; force active mode |
| `IsDispatchActive` | True while on the active `IntervalTimer` path |
| `Track` / `Release` | Dispatcher membership only (not lifecycle parent) |
| `Update()` | Attached + `HasModelChanges` → `ApplyModelChanges` at `DispatcherPriority.Render`; returns whether any applied |

Source: `Cornerstone/Presentation/ApplicationViewModel.cs`, `AdaptiveDispatchMode.cs`, `IAppDispatcher.cs`.

---

## TextIngress

One-way double-buffered character ingress for high-rate producers (LLM tokens).

```
Producers (any thread)          Consumer (dispatch tick / TrackIngress)
─────────────────────          ──────────────────────────────────────
Append(...)  →  HasPending     Drain / DrainTo → destination
```

| API | Role |
|-----|------|
| `Append(...)` | Stage under lock |
| `HasPending` / `PendingCount` | Lock-free poll |
| `Drain` / `DrainTo` | Swap buffers; consumer outside lock |
| `Clear()` | Drop staged data without applying |
| `ClearHasPending()` | No-op (pending is the staged count) |

Optional: after a burst of `Append`, call `IAppDispatcher.RequestDispatch()` so the UI drains at active rate without waiting for the idle period.

Unit coverage: `Tests/Cornerstone.UnitTests/Text/TextIngressTests.cs`.

---

## How the pieces fit

```
ApplicationViewModel (IAppDispatcher)
  idle:  ManualResetEventSlim.Wait(IdleInterval | RequestDispatch)
  active: IntervalTimer.WaitForNextTickAsync (RequestDispatch keeps streak)
    → Update()
        foreach tracked DispatchableViewModel
          if IsAttached && HasModelChanges()
              Dispatch(ApplyModelChanges)
    → AdaptiveDispatchMode (applied | requested → active; N empty → dispose timer, idle)

DispatchableViewModel.ApplyModelChanges
  ├── TrackIngress      → TextIngress.Drain
  ├── TrackCollection   → snapshot + ReconcileList* + ClearHasPending
  ├── TrackBinding      → custom + ClearHasPending
  ├── TrackSeries       → CopyFrom / Publish + ClearHasPending (derived series)
  ├── TrackProperties   → mapped property projection
  ├── TrackDerived      → after any of the above applied (or first-tick seed)
  ├── TrackIntent       → not in this loop; user PropertyChanged → publish (skipped while applying)
  └── DispatchableViewModel<T> → Model.ApplyChangesTo(this)
```

**Membership vs lifecycle:** Host `Track` / `Release` are lifecycle only. Apply-loop membership is `Attach` / `Detach` (views do this; that calls `IAppDispatcher.Track` / `Release`). Docked tabs are lifecycle children of **DockingManager** (see [Controls/DockingLifecycle.md](Controls/DockingLifecycle.md)) — do not also `Track` them on the app ViewModel.

---

## Current progress & adoption

### Implemented (core)

| Item | Status |
|------|--------|
| `IDispatchPending` + `DispatchPending` | Done |
| `SpeedyList` structural pending | Done |
| `TextIngress` as `IDispatchPending` | Done (clear is no-op) |
| `TrackIngress` / `TrackCollection` / `TrackBinding` / `TrackSeries` | Done |
| Projected `TrackCollection` `same` / `create` / `update` / `remove` | Done |
| `ReleaseTracks` (drop apply recipe; UninitializeLifecycle already calls this) | Done |
| `TrackProperties` / `IPropertyMap` (one-way + two-way, rename, convert, `TContract` auto-map) | Done |
| `TrackDerived` (seed, then follow sibling applies; does not consume pending) | Done |
| `TrackIntent` (user property change → bus; suppressed while applying) | Done |
| Virtual Has/Apply composition on DVM / DVM`<T>` | Done |
| Adaptive idle/active + `RequestDispatch` + park idle / `IntervalTimer` active | Done |
| Unit tests (pending, bindings, ingress, property maps, adaptive mode) | Done |

### Sample app

Tab **AppDispatcher** shell hosts one demo **View + ViewModel** at a time (selected content). Attach/detach follows the visual tree — no host-level `Attach` for child demos.

| Surface | View | ViewModel |
|---------|------|-----------|
| Automatic | `TabAppDispatcherAutomaticView` | Host + `TabAppDispatcherTestViewModel` (attach teaching via nested View) |
| Streaming | `TabAppDispatcherStreamingView` | `TabAppDispatcherStreamingViewModel` (`TrackIngress`) |
| Collections | `TabAppDispatcherCollectionsView` | `TabAppDispatcherCollectionsViewModel` (`TrackCollection` → `PresentationList`) |
| Properties | `TabAppDispatcherPropertiesView` | `TabAppDispatcherPropertiesViewModel` (`TrackProperties`) |

Charts: **Model** = mutation sites (`Profiler.Time("Model", …)`); **View** = `AppDispatcher.Apply` via `SystemProfiler` while the shell is attached. Leaving a demo tab detaches that demo’s dispatchable so apply stops even if the model keeps updating.

Sample **producers** may still use `IntervalTimer` at very high rates (e.g. 2000 Hz) to generate model traffic; that is independent of the app shell’s adaptive poll.

### Agent app

| Piece | Usage |
|-------|--------|
| `AppViewModel` | `IAppDispatcher`, adaptive defaults (idle 10 park / active 120 IntervalTimer / N=8) |
| `AgentViewModel` | `TrackIngress`; `TrackCollection` Models; `TrackProperties` Settings.SelectedModel ↔ SelectedModel (string ↔ ModelInfo) |
| `SettingsViewModel` | `DispatchableViewModel<AppSettings>` + `AutoUpdateModel` |
| GrokMonitor usage tab | `TrackProperties<IGrokHomeUsage>` + leftover convert / `TrackCollection` (sessions, periods) / `TrackBinding` for charts / `TrackDerived(ProjectDerived)` / `TrackIntent` (period combo, view-clock slider) |
| GrokMonitor host | Child `HomeTabProjection`: `TrackCollection(Homes → HomeTabs, create, update, remove: Release)` + `TrackDerived` (Settings last) |

### Gaps / next steps

- Remaining `ModelState` ingress streams when UI surfaces exist
- Optional auto-`RequestDispatch` from projected collection factories (factory overload is implemented)
- Optional auto-`RequestDispatch` from `IDispatchPending.MarkPending` / ingress (latency only; poll remains correct)
- Per-binding `Untrack*` if sources outlive the VM
- Item-level dirty without list mutation (document / optional bump)
- Optional: pause high-rate model producers when `IsAttached` becomes false

---

## Usage sketch

```csharp
public partial class AgentViewModel : DispatchableViewModel
{
    public AgentViewModel(AppBus bus, AppState state /*, IAppDispatcher appDispatcher */)
    {
        Models = [];
        Output = new TextEditorViewModel();

        TrackIngress(state.ModelState.ModelIngress, Output.Append);
        TrackIngress(state.ModelState.OutputIngress, Output.Append);
        TrackCollection(state.ModelState.Models, Models, pathComparer, CollectionReconcileMode.List);
        // No Has/Apply override needed when bindings cover everything
    }
}

// After staging tokens (any thread) — optional latency boost:
// appDispatcher.RequestDispatch();

// Scalar model page
public class SettingsPage : DispatchableViewModel<AppSettings>
{
    public SettingsPage(AppSettings model) : base(model)
    {
        AutoUpdateModel = true;
    }
}

// Custom pending work
var chartPending = new DispatchPending();
TrackBinding(chartPending, () => ViewSeries.CopyFrom(ModelSeries));
```

Producers:

```csharp
state.OutputIngress.Append(token);           // any thread
state.ModelState.Models.ReconcileList(...);  // marks SpeedyList.HasPending
// optional: appDispatcher.RequestDispatch();
```

---

## Troubleshooting

| Symptom | Likely cause | What to check |
|---------|--------------|---------------|
| Steady ~few % CPU, UI idle | Old fixed high-Hz timer or other spin loops | Confirm adaptive defaults; apply rate near 0; `IsDispatchActive` mostly false |
| UI lags ~100 ms on first change | Idle poll only (no request) | Call `RequestDispatch` after staging, or accept idle latency |
| UI still high CPU while “idle” | Always-dirty model / binding | `SystemProfiler` apply rate; `HasModelChanges` never clears |
| Tab updates after close | Still tracked **and** attached | Docking `Release` + visual detach; membership vs lifecycle |
| No updates at all | Not tracked, not attached, or lifecycle not started | `Track`, visual tree `Attach`, `StartLifecycle` on app VM |

---

## Related documentation

| Document | Relationship |
|----------|----------------|
| [ViewIntegration.md](ViewIntegration.md) | Manual / custom UI integration (no requirement to use AppDispatcher) |
| [Lifecycle.md](Lifecycle.md) | Track / Release and parent/child lifecycle order |
| [Keystone.md](Keystone.md) | Bus : State : Engine — models that feed the dispatcher |
| [KeystoneFeatureTab.md](KeystoneFeatureTab.md) | How-to: dockable feature tab using Keystone + this layer |
| [Controls/DockingLifecycle.md](Controls/DockingLifecycle.md) | Docking owns tab lifecycle; views Attach for apply |
| [CornerstoneApplication.md](CornerstoneApplication.md) | App shell lifecycle and Avalonia hosting |
| [Controls/MarkdownView.md](Controls/MarkdownView.md) | Document buffer as a common drain destination for streaming text |
