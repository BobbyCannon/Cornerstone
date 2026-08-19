# Docking lifecycle

Tabs (`DockableTabModel`) are lifecycle objects. **`DockingManager` (root) owns their session** via a `LifecycleTracker`.

## Rules

1. **Do not** call `InitializeLifecycle` / `LoadLifecycle` / `StartLifecycle` / `UninitializeLifecycle` on tab models from feature code when docking them.
2. Use **`DockingManager.Add`**, **`ReplaceTab`**, or tab-control **`Add`/`Insert`** — these call **`ActivateTab`**.
3. Close paths call **`DeactivateTab`** (Stop → Unload → Uninitialize).
4. Host `Track`s `DockingManager.TabLifecycle` so parent Initialize → Load → Start cascade.
5. Floating windows share the **root** manager’s tab lifecycle (Activate/Deactivate always use `RootDockingManager`).

## API

| Method | Role |
|--------|------|
| `ActivateTab(model)` | `Track` on tab lifecycle + `IAppDispatcher.Track` if dispatchable |
| `DeactivateTab(model)` | `IAppDispatcher.Release` + lifecycle `Release` |
| `DeactivateAllTabs()` | Tear down every owned tab (app shutdown) |

## AppDispatcher

`PopupManager` → `DispatchableViewModel`, so dockable tabs can use `TrackCollection` / `TrackProperties`. The tab view `Attach`es the VM into the apply loop.

**IsAttached:** AppDispatcher only applies while `IsAttached`. `DockableTabView` attaches the tab model when the header is on the visual tree (and detaches on leave). Content views may also attach via `DispatchableVisualTree` (multi-owner).

**Track vs Attach:** `Track` is lifecycle (DockingManager’s `LifecycleTracker`). `Attach` is apply-loop membership.

**Rates:** the host’s AppDispatcher parks idle (~10 Hz) and uses `IntervalTimer` while active (default ~120 Hz). See [AppDispatcher.md](../AppDispatcher.md).

## Related

- [Lifecycle.md](../Lifecycle.md)
- [KeystoneFeatureTab.md](../KeystoneFeatureTab.md) — full recipe for a Keystone + AppDispatcher document tab
- [AppDispatcher.md](../AppDispatcher.md)