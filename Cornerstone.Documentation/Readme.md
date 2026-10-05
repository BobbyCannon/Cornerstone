# Cornerstone Documentation

Cornerstone is a shared .NET framework for desktop and cross-platform apps: process bootstrap, Keystone application architecture, lifecycle, optional UI projection, controls, parsing, and runtime utilities.

This index covers how the framework behaves and how hosts use it.

---

## Architecture and application shell

| Document | Summary |
|----------|---------|
| [AppBootstrap.md](AppBootstrap.md) | Process bootstrap — DI root, runtime info, platform, infrastructure lifecycle |
| [Keystone.md](Keystone.md) | Bus : State : Engine — application business logic, off the UI dispatcher |
| [KeystoneFeatureTab.md](KeystoneFeatureTab.md) | How-to: new dockable feature tab with Keystone + AppDispatcher (samples: `Cornerstone.GrokMonitor`, `Cornerstone.RemoteLink`) |
| [Lifecycle.md](Lifecycle.md) | Lifecycle phases and LifecycleTracker (parent/child order, track/release) |
| [CornerstoneApplication.md](CornerstoneApplication.md) | How a Cornerstone app hosts Keystone and wires startup lifecycle |
| [ViewIntegration.md](ViewIntegration.md) | Keystone State → MVVM; automatic `CornerstoneUserControl` Attach/Detach |
| [AppDispatcher.md](AppDispatcher.md) | State → ViewModel for display and input; `Track*` bindings including `TrackDerived` |
| [Diagnostics.md](Diagnostics.md) | Opt-in developer monitoring: bus history, AppDispatcher snapshots, Profiler (Sample-first) |

## Runtime utilities

| Document | Summary |
|----------|---------|
| [DebounceAndThrottle.md](DebounceAndThrottle.md) | Simple Debounce/Throttle vs DebounceThrottleManager |
| [Logging.md](Logging.md) | In-memory circular Logger vs structured Tracker |
| [RuntimeInformation.md](RuntimeInformation.md) | Process snapshot: application identity, device facts, cache, and lifecycle |
| [Serializer.md](Serializer.md) | JSON serialize/deserialize, CreateOptions forks, file streaming |
| [TokenTextFilter.md](TokenTextFilter.md) | Whitespace-token AND filter across any text fields (Sample tab) |
| [Sync.md](Sync.md) | Entity sync: `SyncId`, pull/push rules, one `POST api/Sync`, SQL apply |
| [Storage.md](Storage.md) | SQL table mapper vs EF: generated CUD, versioned migrations, Sample Loopback (`WebSyncClient`) |
| [SourceReflection.md](SourceReflection.md) | Compile-time type maps for Native AOT; no runtime reflection on the generated path |

## Appearance

| Document | Summary |
|----------|---------|
| [Themes.md](Themes.md) | Color, light/dark mode, UI density, and bundled font folder vs file URIs |

## Presentation (UI runtime)

`Cornerstone.Presentation` is the UI runtime. Full index: [Presentation/Readme.md](Presentation/Readme.md). Default theme and product **controls** stay under Controls/.

| Document | Summary |
|----------|---------|
| [Presentation/Readme.md](Presentation/Readme.md) | Runtime vs Theme; index of Presentation pages |
| [Presentation/Projects.md](Presentation/Projects.md) | Each Presentation* project: role, who uses it, NuGet vs repo-only |
| [Presentation/NativeLayering.md](Presentation/NativeLayering.md) | Native views under Skia; holes; Windows frame + layered composition window |
| [Presentation/XamlCompilation.md](Presentation/XamlCompilation.md) | BuildTasks: compile `.cxaml`, pack resources, compiled bindings |
| [Presentation/Upstream.md](Presentation/Upstream.md) | Third-party source notice |

## Controls

Cornerstone.Presentation controls under `Cornerstone.Presentation.Theme`. Full index: [Controls/Readme.md](Controls/Readme.md).

| Document | Summary |
|----------|---------|
| [Controls/DockingLifecycle.md](Controls/DockingLifecycle.md) | DockingManager owns tab Init/Load/Start and AppDispatcher Track/Release |
| [Controls/DocumentationReader.md](Controls/DocumentationReader.md) | In-app docs reader: catalog, link/header navigation, hosts |
| [Controls/MarkdownView.md](Controls/MarkdownView.md) | Markdown parser, streaming fences, MarkdownView document model |
| [Controls/TreeDataGrid.md](Controls/TreeDataGrid.md) | TreeDataGrid: MinRowHeight, virtualization, row height rules |

## How To

Task recipes for hosts. Full index: [HowTo/Readme.md](HowTo/Readme.md).

| Document | Summary |
|----------|---------|
| [HowTo/Readme.md](HowTo/Readme.md) | Index of how-tos |
| [HowTo/DocumentationReaderHost.md](HowTo/DocumentationReaderHost.md) | Stand up a documentation WinExe with `DocumentationReaderHost` |

## Quick Start

Short paths through existing code. Full index: [QuickStart/Readme.md](QuickStart/Readme.md).

| Document | Summary |
|----------|---------|
| [QuickStart/Readme.md](QuickStart/Readme.md) | Index of quick starts |
| [QuickStart/HeadlessTesting.md](QuickStart/HeadlessTesting.md) | Run UI tests without a window: session, starter tests, and the two test projects |

## Build, tests, and known issues

| Document | Summary |
|----------|---------|
| [Build.md](Build.md) | MSBuild configuration order (Directory.Build.props → project → platform) |
| [NuGet.md](NuGet.md) | The seven packages that publish, and what stays source-only |
| [Testing.md](Testing.md) | MSTest projects, generated `Main`, CS5001 / CSG003 |
| [Todo/HeadlessControlCoverage.md](Todo/HeadlessControlCoverage.md) | Open queue: headless property and feature tests for every Presentation control |
| [Todo/SingleRoslynGenerator.md](Todo/SingleRoslynGenerator.md) | Open: fold DevGenerators into Cornerstone.Generators after the ungated triggers are opt-in |
| [KnownIssues.md](KnownIssues.md) | Tracked platform and framework issues |