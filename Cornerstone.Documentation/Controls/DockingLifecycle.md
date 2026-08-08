# Docking lifecycle

Tabs (`DockableTabModel`) are lifecycle objects. **`DockingManager` (root) owns their session** via a `LifecycleTracker`.

## Rules

1. **Do not** call `InitializeLifecycle` / `LoadLifecycle` / `StartLifecycle` / `UninitializeLifecycle` on tab models from feature code when docking them.
2. Use **`DockingManager.Add`**, **`ReplaceTab`**, or tab-control **`Add`/`Insert`** — these call **`ActivateTab`**.
3. Close paths call **`DeactivateTab`** (Stop → Unload → Uninitialize + AppDispatcher.Release).
4. Host cascades docking host phases: `AppViewModel` calls `DockingManager.InitializeLifecycle` / `Load` / `Start` / … in parallel with its own phases.
5. Floating windows share the **root** manager’s tab lifecycle (Activate/Deactivate always use `RootDockingManager`).

## API

| Method | Role |
|--------|------|
| `ActivateTab(model)` | `Track` on tab lifecycle + `IAppDispatcher.Track` if dispatchable |
| `DeactivateTab(model)` | `IAppDispatcher.Release` + `Release` (full reverse lifecycle) |
| `DeactivateAllTabs()` | Tear down every owned tab (app shutdown) |
| `AppDispatcher` | Set by host before opening tabs |

## AppDispatcher

`PopupManager` → `DispatchableViewModel`, so dockable tabs can use `TrackCollection` / `TrackProperties`. Coupling happens in Activate/Deactivate — tabs should **not** call `IAppDispatcher.Track` themselves.

**IsAttached:** AppDispatcher only applies while `IsAttached`. `DockableTabView` attaches the tab model when the header is on the visual tree (and detaches on leave). Content views may also attach via `DispatchableVisualTree` (multi-owner).

**Track vs lifecycle:** `IAppDispatcher.Track` is membership for the apply loop only (not a second lifecycle parent). Tab Init/Load/Start remains solely under `DockingManager`’s `LifecycleTracker`.

## Related

- [Lifecycle.md](../Lifecycle.md)
- Editor SC: `Documentation/EpicCoders/Cornerstone.Editor/SourceControl.Keystone.md`