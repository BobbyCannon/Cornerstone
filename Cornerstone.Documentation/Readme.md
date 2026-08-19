# Cornerstone Documentation

Cornerstone is a shared .NET framework for desktop and cross-platform apps: process bootstrap, Keystone application architecture, lifecycle, optional UI projection, controls, parsing, and runtime utilities.

This index covers how the framework behaves and how hosts use it.

---

## Architecture and application shell

| Document | Summary |
|----------|---------|
| [AppBootstrap.md](AppBootstrap.md) | Process bootstrap — DI root, runtime info, platform, infrastructure lifecycle |
| [Keystone.md](Keystone.md) | Bus : State : Engine — application business logic, off the UI dispatcher |
| [KeystoneFeatureTab.md](KeystoneFeatureTab.md) | How-to: new dockable feature tab with Keystone + AppDispatcher (sample: `Cornerstone.GrokMonitor`) |
| [Lifecycle.md](Lifecycle.md) | Lifecycle phases and LifecycleTracker (parent/child order, track/release) |
| [CornerstoneApplication.md](CornerstoneApplication.md) | How an Avalonia app hosts Keystone and wires startup lifecycle |
| [ViewIntegration.md](ViewIntegration.md) | Keystone State → MVVM; automatic `CornerstoneUserControl` Attach/Detach |
| [AppDispatcher.md](AppDispatcher.md) | State → ViewModel for display and input; `Track*` bindings including `TrackDerived` |
| [Diagnostics.md](Diagnostics.md) | Opt-in developer monitoring: bus history, AppDispatcher snapshots, Profiler (Sample-first) |

## Runtime utilities

| Document | Summary |
|----------|---------|
| [DebounceAndThrottle.md](DebounceAndThrottle.md) | Simple Debounce/Throttle vs DebounceThrottleManager |
| [Logging.md](Logging.md) | In-memory circular Logger vs structured Tracker |
| [Serializer.md](Serializer.md) | JSON serialize/deserialize, CreateOptions forks, file streaming |
| [TokenTextFilter.md](TokenTextFilter.md) | Whitespace-token AND filter across any text fields (Sample tab) |

## Appearance

| Document | Summary |
|----------|---------|
| [Themes.md](Themes.md) | Color, light/dark mode, and UI density; DynamicResource tokens for chrome |

## Controls

Avalonia controls under `Cornerstone.Avalonia`. Full index: [Controls/Readme.md](Controls/Readme.md).

| Document | Summary |
|----------|---------|
| [Controls/DockingLifecycle.md](Controls/DockingLifecycle.md) | DockingManager owns tab Init/Load/Start and AppDispatcher Track/Release |
| [Controls/DocumentationReader.md](Controls/DocumentationReader.md) | In-app docs reader: catalog, link/header navigation, hosts |
| [Controls/MarkdownView.md](Controls/MarkdownView.md) | Markdown parser, streaming fences, MarkdownView document model |
| [Controls/TreeDataGrid.md](Controls/TreeDataGrid.md) | TreeDataGrid: MinRowHeight, virtualization, row height rules |

## Build and known issues

| Document | Summary |
|----------|---------|
| [Build.md](Build.md) | MSBuild configuration order (Directory.Build.props → project → platform) |
| [KnownIssues.md](KnownIssues.md) | Tracked platform and framework issues |
| [Todo/AppBootstrapFence.md](Todo/AppBootstrapFence.md) | Planned fence so feature code cannot service-locate via AppBootstrap |
| [Todo/GrokUsagePattern.md](Todo/GrokUsagePattern.md) | Remaining polish so GrokMonitor usage stays the Keystone + AppDispatcher example |
| [Todo/Sync.md](Todo/Sync.md) | Engineering review notes for the sync stack |