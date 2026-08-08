# AppDispatcher

AppDispatcher is an **optional auto layer** that keeps **ViewModels in sync with models** (typically Keystone **State**) on a hard, deterministic tick loop. It only processes ViewModels that are **attached to a View**, so detached UI pays no update cost.

Manual / custom UI integration remains fully valid without this layer — see [ViewIntegration.md](ViewIntegration.md).

---

## Layering

| Layer | Role |
|-------|------|
| **ViewIntegration** | How any UI can attach to Keystone State (manual wiring allowed) |
| **AppDispatcher** | Optional app loop: track attached `DispatchableViewModel`s → `HasModelChanges` / `ApplyModelChanges` |
| **Dispatch bindings** | Optional *inside* a ViewModel: `TrackIngress` / `TrackCollection` / `TrackBinding` driven by `IDispatchPending` |

---

## Goals

| Goal | How it is achieved |
|------|--------------------|
| Deterministic UI projection | Fixed interval worker (default **120 Hz**) on `ApplicationViewModel` |
| Skip invisible UI | Multi-owner `Attach`/`Detach`; `IsAttached` when any owner remains |
| Cheap pending checks | `IDispatchPending.HasPending` / binding `HasPending()` — O(bindings), not O(list) |
| Coalesce high-rate text | `TextIngress` stages tokens; `TrackIngress` drains once per tick |
| Coalesce list membership | `SpeedyList` marks pending; `TrackCollection` reconciles into presentation lists |
| Keep models UI-free | Models mutate freely; ViewModels pull/apply on the dispatch thread |

---

## Foundation: `IDispatchPending`

Coarse **“view needs an update”** signal for the dispatch poll. **Not** a substitute for `ITrackPropertyChanges` (property-bit graphs / `ApplyChangesTo`).

```csharp
// Cornerstone/Presentation/IDispatchPending.cs
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
TrackBinding(pending, () => viewSeries.CopyFrom(modelSeries));
```

**Relation to property change tracking**

| | `ITrackPropertyChanges` | `IDispatchPending` |
|--|-------------------------|--------------------|
| Granularity | Per-property bits | One coarse flag / buffered count |
| Apply model | `ApplyChangesTo` / `UpdateWith` | Caller projection (drain, reconcile, custom) |
| Clear API | `ResetHasChanges()` | `ClearHasPending()` |

List pending covers **membership / order / slot replace**. Deep property edits on items without list mutation are a separate concern (`ReconcileListAndItems` + item `HasChanges`, or scalar model apply).

---

## Core types

### `IAppDispatcher`

```csharp
public interface IAppDispatcher
{
    Profiler SystemProfiler { get; set; }  // null = no system profiling cost
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
    └── TrackProperties(model) → IPropertyMap (MapOneWay / MapTwoWay)

DispatchableViewModel<T> : DispatchableViewModel
    where T : IUpdateable, ITrackPropertyChanges
    ├── Model / AutoUpdateModel
    ├── HasModelChanges()  → base || Model.HasChanges()
    └── ApplyModelChanges() → base then Model.ApplyChangesTo(this)
```

**`Attach` / `Detach` / `IsAttached`**

- **Owner is required** (not null): a **View** (`Attach(this)`) or a **parent** dispatchable cascading to children. There is no anonymous / null-owner attach.
- Idempotent per owner; `IsAttached` is true while **any** owner remains (not a single bool flip).
- Parent registers nested VMs with `TrackDispatchChild`; on 0→1 attach it calls `child.Attach(parent)`, on last detach `child.Detach(parent)`.
- Avalonia bases (`CornerstoneUserControl`, `Control`, `ContentControl`, `TemplatedControl`, `Window`, `AppView`) call `Attach(this)` / `Detach(this)` from **visual tree** attach/detach for **ViewModel and DataContext independently** (via `DispatchableVisualTree`). They never set or clear those properties for this purpose.
- AppDispatcher polls only **direct** tracked roots (`IsAttached` + `HasModelChanges`). Nested work flows down: each `ApplyModelChanges` applies itself then its **direct** `TrackDispatchChild` children (no grand-child collection).

### Bindings (`IDispatchBinding`)

Registered in the ViewModel constructor (or later). Owned by the VM; **not** registered with `IAppDispatcher`.

| API | Behavior |
|-----|----------|
| `TrackIngress(TextIngress, Action<ReadOnlySpan<char>>)` | Drain consumer when `HasPending` |
| `TrackIngress(TextIngress, IStringBuffer)` | `DrainTo` destination |
| `TrackCollection(source, dest, comparer?, mode?)` | Source must be `IList<T>` **and** `IDispatchPending`; snapshot + `ReconcileList` / `ReconcileListAndItems` |
| `TrackBinding(IDispatchPending, Action)` | Custom apply then `ClearHasPending` |
| `TrackBinding(IDispatchBinding)` | Fully custom binding |
| `TrackProperties(ITrackPropertyChanges)` | Property-to-property map (rename / convert / two-way); see below |

### Property maps (`TrackProperties`)

Use when the ViewModel is **not** a 1:1 `DispatchableViewModel<T>` / shared interface, but still needs selected model fields projected on the dispatch tick.

| Need | Prefer |
|------|--------|
| Full settings page, same names/types | `DispatchableViewModel<AppSettings>` + `AutoUpdateModel` |
| Partial slice, rename, or type convert | `TrackProperties(model).Map…` |
| Lists | `TrackCollection` |
| High-rate text | `TrackIngress` |

```csharp
// Two-way: settings path (string) ↔ VM selection (ModelInfo); names may differ
TrackProperties(state.Settings)
    .MapTwoWay(
        nameof(AppSettings.SelectedModel),   // or FavoriteModel
        nameof(SelectedModel),
        path => ResolveModel(path),          // string → ModelInfo
        model => model?.FilePath);           // ModelInfo → string

// One-way display flags
TrackProperties(state.ModelState)
    .MapOneWay(nameof(ModelState.IsModelLoading), nameof(IsModelLoading), x => x);
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

**Collection modes** (`CollectionReconcileMode`)

- `List` — add/remove only (`ReconcileList`)
- `ListAndItems` — add/remove/update/order (`ReconcileListAndItems`; presentation overload when dest is `IPresentationList<T>`)

Core stays Avalonia-free: pass `Output.Append` for editors so document-change events fire (do not drain into the raw gap buffer).

### `ApplicationViewModel`

| Responsibility | Behavior |
|----------------|----------|
| Worker loop | `StartLifecycle` / `StopLifecycle` + `IntervalTimer` |
| Tick rate | `updatesPerSecond` (default **120**) |
| `Track` / `Release` | Lifecycle children |
| `Update()` | Attached + `HasModelChanges` → `ApplyModelChanges` at `DispatcherPriority.Render` |

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

Unit coverage: `Tests/Cornerstone.UnitTests/Text/TextIngressTests.cs`.

---

## How the pieces fit

```
ApplicationViewModel (IAppDispatcher)
  IntervalTimer @ N Hz → Update()
    foreach tracked DispatchableViewModel
      if IsAttached && HasModelChanges()
          Dispatch(ApplyModelChanges)

DispatchableViewModel.ApplyModelChanges
  ├── TrackIngress      → TextIngress.Drain
  ├── TrackCollection   → snapshot + ReconcileList* + ClearHasPending
  ├── TrackBinding      → custom + ClearHasPending
  └── DispatchableViewModel<T> → Model.ApplyChangesTo(this)
```

---

## Current progress & adoption

### Implemented (core)

| Item | Status |
|------|--------|
| `IDispatchPending` + `DispatchPending` | Done |
| `SpeedyList` structural pending | Done |
| `TextIngress` as `IDispatchPending` | Done (clear is no-op) |
| `TrackIngress` / `TrackCollection` / `TrackBinding` | Done |
| `TrackProperties` / `IPropertyMap` (one-way + two-way, rename, convert) | Done |
| Virtual Has/Apply composition on DVM / DVM`<T>` | Done |
| Unit tests (pending, bindings, ingress, property maps) | Done |

### Sample app

Tab **AppDispatcher** shell hosts one demo **View + ViewModel** at a time (selected content). Attach/detach follows the visual tree — no host-level `Attach` for child demos.

| Surface | View | ViewModel |
|---------|------|-----------|
| Automatic | `TabAppDispatcherAutomaticView` | Host + `TabAppDispatcherTestViewModel` (attach teaching via nested View) |
| Streaming | `TabAppDispatcherStreamingView` | `TabAppDispatcherStreamingViewModel` (`TrackIngress`) |
| Collections | `TabAppDispatcherCollectionsView` | `TabAppDispatcherCollectionsViewModel` (`TrackCollection` → `PresentationList`) |
| Properties | `TabAppDispatcherPropertiesView` | `TabAppDispatcherPropertiesViewModel` (`TrackProperties`) |

Charts: **Model** = mutation sites (`Profiler.Time("Model", …)`); **View** = `AppDispatcher.Apply` via `SystemProfiler` while the shell is attached. Leaving a demo tab detaches that demo’s dispatchable so apply stops even if the model keeps updating.

### Agent app

| Piece | Usage |
|-------|--------|
| `AppViewModel` | `IAppDispatcher`, 120 Hz |
| `AgentViewModel` | `TrackIngress`; `TrackCollection` Models; `TrackProperties` Settings.SelectedModel ↔ SelectedModel (string ↔ ModelInfo) |
| `SettingsViewModel` | `DispatchableViewModel<AppSettings>` + `AutoUpdateModel` |

### Gaps / next steps

- Remaining `ModelState` ingress streams when UI surfaces exist
- Projected collection bindings (model item → row ViewModel factory)
- `Untrack*` / binding lifecycle cleanup if sources outlive the VM
- Item-level dirty without list mutation (document / optional bump)
- Optional: pause high-rate model producers when `IsAttached` becomes false

---

## Usage sketch

```csharp
public partial class AgentViewModel : DispatchableViewModel
{
    public AgentViewModel(AppBus bus, AppState state)
    {
        Models = [];
        Output = new TextEditorViewModel();

        TrackIngress(state.ModelState.ModelIngress, Output.Append);
        TrackIngress(state.ModelState.OutputIngress, Output.Append);
        TrackCollection(state.ModelState.Models, Models, pathComparer, CollectionReconcileMode.List);
        // No Has/Apply override needed when bindings cover everything
    }
}

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
```

---

## Related documentation

| Document | Relationship |
|----------|----------------|
| [ViewIntegration.md](ViewIntegration.md) | Manual / custom UI integration (no requirement to use AppDispatcher) |
| [Lifecycle.md](Lifecycle.md) | Track / Release and parent/child lifecycle order |
| [Keystone.md](Keystone.md) | Bus : State : Engine — models that feed the dispatcher |
| [CornerstoneApplication.md](CornerstoneApplication.md) | App shell lifecycle and Avalonia hosting |
| [Controls/MarkdownView.md](Controls/MarkdownView.md) | Document buffer as a common drain destination for streaming text |