# Cornerstone Documentation

Cornerstone is a shared .NET framework for desktop and cross-platform apps: process bootstrap, Keystone application architecture, lifecycle, optional UI projection, controls, parsing, and runtime utilities.

This index covers how the framework behaves and how hosts use it.

---

## Architecture and application shell

| Document | Summary |
|----------|---------|
| [AppBootstrap.md](AppBootstrap.md) | Process bootstrap — DI root, runtime info, platform, infrastructure lifecycle |
| [Keystone.md](Keystone.md) | Bus : State : Engine architecture and feature-slice layout |
| [KeystoneFeatureTab.md](KeystoneFeatureTab.md) | How-to: new dockable feature tab with Keystone + AppDispatcher (sample: `Cornerstone.GrokMonitor`) |
| [Lifecycle.md](Lifecycle.md) | Lifecycle phases and LifecycleTracker (parent/child order, track/release) |
| [CornerstoneApplication.md](CornerstoneApplication.md) | How an Avalonia app hosts Keystone and wires startup lifecycle |
| [ViewIntegration.md](ViewIntegration.md) | Manual Keystone State → MVVM integration without AppDispatcher |
| [AppDispatcher.md](AppDispatcher.md) | Adaptive idle park + active IntervalTimer, RequestDispatch, TrackIngress / TrackCollection, IsAttached gating |
| [Diagnostics.md](Diagnostics.md) | Opt-in developer monitoring: bus history, AppDispatcher snapshots, Profiler (Sample-first) |

## Runtime utilities

| Document | Summary |
|----------|---------|
| [DebounceAndThrottle.md](DebounceAndThrottle.md) | Simple Debounce/Throttle vs DebounceThrottleManager |
| [Logging.md](Logging.md) | In-memory circular Logger vs structured Tracker |
| [Serializer.md](Serializer.md) | JSON serialize/deserialize, CreateOptions forks, file streaming |

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
| [Todo/Sync.md](Todo/Sync.md) | Engineering review notes for the sync stack |