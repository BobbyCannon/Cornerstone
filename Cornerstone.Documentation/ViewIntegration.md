## View Integration

Keystone is designed to serve as the **Model** layer in a classic MVVM architecture.

Because the Keystone **State** is the single source of truth and is deliberately free of UI concerns, any UI framework can attach to it as the Model.  
ViewModels then become thin, reactive adapters that expose the State (or projected slices of it) in a form that is convenient for binding to Views.

This keeps the domain logic, mutation rules, and communication completely isolated from the presentation layer while still allowing the UI to stay perfectly in sync with the underlying State.

### Two valid approaches

| Approach | When |
|----------|------|
| **Manual** | Custom events, property change handlers, one-off screens — no AppDispatcher required |
| **AppDispatcher** | Optional auto poll: attached `DispatchableViewModel`s project pending model work on an adaptive tick |

Full AppDispatcher behavior (idle/active rates, `RequestDispatch`, bindings): [AppDispatcher.md](AppDispatcher.md).

### Cornerstone AppDispatcher (optional)

The **AppDispatcher** (`IAppDispatcher` / `ApplicationViewModel`) is the optional bridge that connects Keystone State to MVVM projection.

It runs an **adaptive poll loop** (not a fixed always-on high-Hz hard tick):

| Mode | Default | Wait | Role |
|------|---------|------|------|
| **Idle** | ~10 updates/s | Parked wait | Cheap safety poll when nothing is dirty |
| **Active** | ~120 updates/s | `IntervalTimer` | Precise high-rate projection while work is flowing or after `RequestDispatch` |

After several consecutive ticks with no apply, the loop returns to idle. Producers may call `RequestDispatch()` to wake early for lower latency; they are not required for correctness.

Every tick the dispatcher walks **tracked** ViewModels and applies only those that are **`IsAttached`** and report `HasModelChanges()`. Detached ViewModels are skipped for apply work.

#### Core responsibilities

- **Adaptive update loop** – Idle/active periods with a parking wait (low idle CPU)
- **Attach filtering** – Only `IsAttached` roots (and their dispatch children on apply) are projected
- **Projection & sync** – Transform Keystone State / pending sources into ViewModel state
- **Membership** – `Track` / `Release` for poll set only (lifecycle is usually DockingManager)

#### Typical flow (per tick)

1. Worker waits for the current period **or** a `RequestDispatch` wake.
2. It iterates tracked roots that are **attached** and have model changes.
3. For each, it runs `ApplyModelChanges` on the UI dispatcher (bindings, property maps, model apply).
4. Mode advances to active if work ran or a request woke the wait; otherwise empty-tick streak may return to idle.
5. Detached or clean ViewModels are skipped for apply.

### Wiring model to the ViewModel

Common automatic paths (see AppDispatcher docs for APIs):

| Need | Prefer |
|------|--------|
| Full model page, same names/types | `DispatchableViewModel<T>` + `AutoUpdateModel` |
| Partial / rename / convert | `TrackProperties(model).Map…` |
| Lists | `TrackCollection` + `IDispatchPending` source |
| High-rate text | `TrackIngress` + `TextIngress` |
| Custom | `TrackBinding` or override `HasModelChanges` / `ApplyModelChanges` |

More complex multi-source ViewModels can still be registered and updated **manually** without AppDispatcher when that is simpler.

### Related

- [AppDispatcher.md](AppDispatcher.md) — adaptive rates, pending, bindings, diagnostics
- [KeystoneFeatureTab.md](KeystoneFeatureTab.md) — dockable tab recipe
- [Keystone.md](Keystone.md) — Bus · State · Engine
