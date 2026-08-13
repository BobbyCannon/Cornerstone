# Keystone feature tab (how-to)

Recipe for adding a **dockable document tab** that uses **Keystone** (Bus · State · Engine) for domain work and **AppDispatcher** to project state into a thin ViewModel/View.

This is the pattern used by **DockingManager** hosts (document tabs such as source control or a shell). **Cornerstone.GrokMonitor** uses the same Bus · State · Processor · AppDispatcher stack but hosts one home dashboard per shell `TabControl` tab (no docking). Framework pieces stay Avalonia-aware only at the View/tab boundary.

---

## When to use this

| Use Keystone + tab | Prefer a plain ViewModel |
|--------------------|---------------------------|
| Domain state shared by more than one surface | One-off dialog or static page |
| Async / process / IO work that must not live in the View | Pure UI layout or local-only chrome |
| Multi-instance resources scoped by id (repo, session, home) | No engine mutations |
| Testable processors without Avalonia | No bus messages |

For manual State → View wiring without the dispatch loop, see [ViewIntegration.md](ViewIntegration.md).

---

## Mental model

```
View / *TabView          thin: selection, layout, publish intent
        │
        ▼
AppBus.*Channel          typed messages (scoped by resource id)
        │
        ▼
*Processor               work → mutate State only
        │
        ▼
AppState.*FeatureState   SpeedyList / CornerstoneObject (UI-free)
        │
        ▼
AppDispatcher            only while tab IsAttached
  TrackCollection / TrackProperties / TrackIngress / TrackBinding
        │
        ▼
*TabViewModel            presentation lists + display properties
```

| Layer | Owns | Does not own |
|-------|------|--------------|
| **State** | Domain snapshot, short status/error strings, list membership | Avalonia controls, scroll/selection |
| **Processor** | IO, parsing, process lifecycle; writes State | Direct UI updates |
| **Bus** | Typed, scoped messages | Business rules |
| **Tab ViewModel** | Which id is focused, layout flags, projection bindings | Domain mutations |
| **DockingManager** | Tab Init/Load/Start/Stop and `IAppDispatcher.Track` / `Release` | Feature logic |

**Rules of thumb**

- UI **publishes** messages; it does not call “god services” for domain work.
- Operations are **scoped by id** (repository, session, home, …). Processors never assume “the active tab.”
- Pass dependencies through **constructors**. Do not use `AppBootstrap.GetInstance` from feature code.
- Docking owns lifecycle and dispatcher membership — tabs do **not** call `InitializeLifecycle` / `IAppDispatcher.Track` when opening themselves.

---

## Feature vertical slice layout

Prefer co-locating the feature under one folder (see [Keystone.md](Keystone.md) feature-slice guidance):

```
FeatureName/
  Channels/           # *Channel, *Messages, *MessageType
  State/              # root *State + row models
  Processors/         # *Processor
  Services/           # optional infrastructure used only by the processor
  *TabViewModel.cs
  *TabView.axaml
  *TabView.axaml.cs

Keystone/             # host composition only
  AppBus.cs           # Track(channel)
  AppState.cs         # feature state property
  AppEngine.cs        # Track(processor)
```

Namespaces should match folders.

---

## State

- Root feature state is typically `[DependencyInjected]` and composed into `AppState`.
- Collections the UI reconciles should be **`SpeedyList<T>`** (implements `IDispatchPending` for membership).
- Row types should be **`CornerstoneObject`** with `[Notifiable]` / `[Updateable]` so `TrackProperties` and `ListAndItems` can see property change bits.
- Keep high-rate text out of full state strings: use **`TextIngress`** and `TrackIngress` (see [AppDispatcher.md](AppDispatcher.md)).
- Prefer empty string / `default` / flags over nullable annotations in new APIs.

---

## Bus and processor

1. Define `*MessageType` enum and small `record struct` payloads implementing `IChannelMessage`.
2. `*Channel : KeystoneChannel<*MessageType>` with publish helpers marked `[ChannelSubscription<…>]`.
3. `*Processor : KeystoneProcessor<AppBus, AppState>` (or host base):
   - Subscribe in `InitializeLifecycle`
   - Unsubscribe in `UninitializeLifecycle`
   - Resolve the state slice by id, do work, update properties/lists
4. Register channel on `AppBus`, processor on `AppEngine`, state on `AppState`.

---

## Document tab checklist

### Naming

| Piece | Pattern | Example |
|-------|---------|---------|
| View model | `*TabViewModel` : `DocumentTabModel` | `GrokUsageTabViewModel` |
| View | `*TabView` | `GrokUsageTabView` |
| Kind id | Stable `TypeId` Guid string | Docking / favorites |
| Icon | `TypeIcon` resource key | `Icons.Chart.Bar` |

`DocumentTabModel` → `DockableTabModel` → `PopupManager` → **`DispatchableViewModel`**, so tabs can use dispatch bindings without a special base.

### View requirements

```csharp
[SourceReflection]
public partial class FeatureTabView : CornerstoneUserControl<FeatureTabViewModel>
{
	[DependencyInjectionConstructor]
	public FeatureTabView()
	{
		// optional design-time ViewModel
		InitializeComponent();
	}
}
```

Without `[SourceReflection]` and a DI constructor, docking / ViewLocator often cannot resolve the control.

### ViewModel

- `[SourceReflection]` + `[DependencyInjected]` (use `TypeLifetime.Transient` when multiple instances are allowed; omit for singleton tools tabs).
- Singleton-style tools often use `base(Guid.Parse(TypeId), "Header", TypeIcon)` so re-open selects the same kind.
- Multi-instance documents (repos, shells) use `Guid.NewGuid()` for the dock instance id and a separate kind `TypeId`.

### Host registration

```csharp
// ApplicationViewModel.InitializeLifecycle (or equivalent)
DockingManager.AppDispatcher = this;
DockingManager.RegisterTab<FeatureTabViewModel>();
```

Open from menu / Getting Started:

```csharp
DockingManager.Add(FeatureTabViewModel.TypeId);
// or CreateTabModel + ReplaceTab / Add(instance)
```

For singleton DI types, `DockingManager.Add(Type)` already focuses an existing tab when present.

### Lifecycle (owned by DockingManager)

| Do | Do not |
|----|--------|
| Publish open/ensure messages from `InitializeLifecycle` if needed | Call `InitializeLifecycle` / `StartLifecycle` yourself when docking |
| Wire `TrackCollection` / `TrackProperties` once when the state target exists | Call `IAppDispatcher.Track` / `Release` yourself |
| Close side effects via bus (e.g. close session) in `UninitializeLifecycle` | Assume the tab is always attached |

Details: [Controls/DockingLifecycle.md](Controls/DockingLifecycle.md).

---

## AppDispatcher projection recipe

Wire bindings on the tab ViewModel (constructor or after the state slice exists):

| Need | API |
|------|-----|
| List membership / item updates | `TrackCollection(sourceSpeedyList, presentationList, comparer, ListAndItems)` |
| Scalar fields (rename/convert) | `TrackProperties(model).MapOneWay` / `MapTwoWay` |
| High-rate text | `TrackIngress(textIngress, appendOrBuffer)` |
| Custom (charts, selected-child projection) | `TrackBinding(pending, action)` or override `HasModelChanges` / `ApplyModelChanges` |

Presentation lists should be constructed with the UI `IDispatcher` so collection notifications stay UI-safe.

**Attach gating:** AppDispatcher only calls `ApplyModelChanges` when `IsAttached` is true (visual tree owners). Detached tabs pay no apply cost even if state keeps updating.

**Adaptive rates:** the app shell parks at **~10 Hz idle** and uses **`IntervalTimer` at ~120 Hz while active** (see [AppDispatcher.md](AppDispatcher.md)). Correctness does not require `RequestDispatch`; call it after high-rate staging if you want the UI to ramp without waiting for the next idle tick (~100 ms).

**Nested hosts:** register child dispatchables with `TrackDispatchChild` (e.g. PowerShell host under a tab).

Sample demos: Cornerstone.Sample `TabAppDispatcher*` surfaces.

---

## Composition root checklist

1. `[DependencyInjected]` channel, state, processor, tab VM (+ view attributes).
2. `AppBus` constructor: inject channel → `Track`.
3. `AppState` constructor: inject feature state → property.
4. `AppEngine` constructor: inject processor → `Track`.
5. Host `RegisterTab<*TabViewModel>()`.
6. Menu / command to open by `TypeId`.

---

## Testing

| Layer | Approach |
|-------|----------|
| Reader / pure services | Temp files, no Keystone |
| Processor | Resolve `AppBus` / `AppState` / processor from test DI; `InitializeLifecycle`; publish messages; assert State |
| Tab projection | Optional; prefer processor tests for domain truth |

Avoid Avalonia in processor tests.

---

## Worked example: Grok Usage (`Cornerstone.GrokMonitor`)

Standalone **desktop-only** sample app: local CLI usage dashboard for discovered Grok homes (`~/.grok*`, env overrides). Host project: `Cornerstone.GrokMonitor/`.

| Piece | Location |
|-------|----------|
| Paths / reader | `GrokUsage/GrokPaths.cs`, `GrokUsageReader.cs` |
| Channel | `GrokUsage/Channels/` |
| State | `GrokUsage/State/` (`GrokUsageState`, `GrokHomeUsageState`, `GrokSessionUsageState`) |
| Processor | `GrokUsage/Processors/GrokUsageProcessor.cs` |
| Tab | `GrokUsageTabViewModel` / `GrokUsageTabView` |
| Composition | `Keystone/AppBus.cs`, `AppState.cs`, `AppEngine.cs` |
| Host | `AppViewModel` (shell tabs + AppDispatcher), `AppWindow` |
| Tests | `Tests/Cornerstone.UnitTests/GrokMonitor/GrokUsage/` |

Flow:

1. Host `StartLifecycle` → `EnsureHomes` → `RefreshAll` → sync shell tabs.
2. Processor discovers existing `~/.grok*` folders (folder-name labels), runs `GrokUsageReader` per home, fills session lists and billing scalars.
3. Each home tab projects via AppDispatcher; shell `TrackBinding(Homes)` keeps tabs in sync when discovery adds homes.
4. Toolbar **Refresh** re-runs discovery, then reloads the focused home and any newly found homes (`RefreshAll` reloads every home).

The same architecture is used in larger DockingManager hosts (multi-instance document tabs scoped by id).

---

## Related documentation

| Document | Role |
|----------|------|
| [Keystone.md](Keystone.md) | Bus · State · Engine and feature-slice layout |
| [AppDispatcher.md](AppDispatcher.md) | Track* APIs, IsAttached, TextIngress |
| [Controls/DockingLifecycle.md](Controls/DockingLifecycle.md) | ActivateTab / DeactivateTab and dispatcher Track/Release |
| [Lifecycle.md](Lifecycle.md) | Track / Release order |
| [ViewIntegration.md](ViewIntegration.md) | Manual projection without AppDispatcher |
| [CornerstoneApplication.md](CornerstoneApplication.md) | Avalonia host lifecycle |
