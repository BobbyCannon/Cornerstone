# Keystone

A lightweight architectural framework built around the **Bus : State : Engine** pattern.

Keystone is the architectural heart of Cornerstone. It provides a clean, high-performance way to organize application logic that is:

- Highly testable  
- Fully observable  
- Easy to reason about  
- Extremely fast  

---

## Core Idea

Keystone organizes systems around three clear responsibilities:

| Component | Responsibility |
|-----------|----------------|
| **State** | The single source of truth (the model) |
| **Engine** | A collection of mostly-stateless processors that manipulate the State |
| **Bus** | A message/channel layer that decouples producers and consumers of change |

This separation keeps mutation logic isolated, communication explicit, and the overall system easier to reason about, test, and extend.

---

## Structure

```
AppKeystone
    ├── AppState
    ├── AppBus
    |   └── Channels
    └── AppEngine
        └── Processors
    
AppKeystone<AppViewModel> : AppKeystone
    ├── ...
    └── ViewModel { get; }
```

### Suggested project layout (optional)

Keystone defines **roles** (State, Bus/Channels, Engine/Processors), not a mandatory folder tree. Physical layout is up to the host app.

**Small apps** often keep everything under a host folder:

```
Keystone/
  AppBus.cs / AppState.cs / AppEngine.cs
  Channels/
  State/
  Processors/
```

**As features grow**, co-locating each feature’s Keystone pieces with its UI and services usually scales better than one global `Channels/` / `State/` / `Processors/` dump. Prefer a **feature vertical slice**; leave `Keystone/` as the **composition root** (bus, root state, engine wiring) plus truly cross-cutting pieces (auth, notifications, shared connectivity, and so on):

```
Keystone/                     # host: AppBus, AppState, AppEngine (+ shared-only channels/state)
FeatureName/                  # e.g. SourceControl, Sync, Browser
  Channels/
  State/
  Processors/
  Services/                   # feature infrastructure (optional name)
  Views / Controls / Popups/  # UI as needed
```

| Prefer under the feature | Prefer under host `Keystone/` |
|--------------------------|--------------------------------|
| Feature channel, messages, message types | `AppBus` / `AppState` / `AppEngine` |
| Feature state types composed into root state | Channels/state used by many features equally |
| Feature processor(s) | Processors that are app-wide infrastructure |
| Feature services used only by that processor | — |

Namespaces should follow folders so “go to type” matches “go to folder.”

This is **guidance**, not a requirement. The architectural contract remains Bus : State : Engine either way. Refactor toward feature slices when navigating files becomes the bottleneck—not before.

### State (`AppState`)
The State is a pure data model that represents the current snapshot of the domain.  
It contains **no behavior** – only structure and (optionally) simple derived values.  

All meaningful changes to the State go through the Engine (processors).

### Engine (`AppEngine`)
The Engine is a set of focused processors (handlers, reducers, or command handlers).  

Each processor:

- Receives a message (command or event) from the Bus  
- Reads the current State  
- Produces a new State (or a partial update)  
- May emit new messages back onto the Bus  

Processors are deliberately kept **mostly stateless**. Any internal state they hold is limited to short-lived working data required for a single operation (caches, temporary buffers, etc.). Persistent or domain state always lives in the State component.

### Bus (`AppBus`)
The Bus provides named channels for asynchronous or synchronous communication.  

Components publish messages onto channels and subscribe to the channels they care about. This decouples the Engine from external actors (UI, other services, timers, etc.) and allows multiple processors or external systems to react to the same event without tight coupling.

**Diagnostics:** set `KeystoneBus.IsHistoryEnabled = true` to record completed publishes into `History` (duration, handler count, errors; bounded by `History.Limit`). Optional `HistoryFilter` text (`channel:… type:0,2 error:true`) restricts what is written to the ring. Off by default. See [Diagnostics.md](Diagnostics.md).

### Lifecycle

Every `AppKeystone` exposes a clear, ordered lifecycle:

1. `InitializeLifecycle()`
1. `LoadLifecycle()`
1. `StartLifecycle()`
1. **`ProcessLifecycle()`** – the continuous main loop
   - Repeatedly calls `CanProcess()` → `Process()` on all registered processable components
   - Continues until a stop is requested
1. `StopLifecycle()`
1. `UnloadLifecycle()`
1. `UninitializeLifecycle()`

This makes startup, shutdown, and resource management deterministic and easy to test.

---

## High-level Flow

1. An external actor (or another processor) publishes a message onto a Bus channel.  
2. One or more Engine processors that subscribe to that channel receive the message.  
3. Each processor reads the current State, applies its logic, and either:  
   - Updates the State, and/or  
   - Publishes follow-up messages onto the Bus.  
4. The new State becomes the new source of truth for subsequent operations.

---

## Hosting Keystone

All hosts start with **[AppBootstrap](AppBootstrap.md)** (process DI, runtime information, platform). Keystone is then registered on that provider and driven through the lifecycle.

### Avalonia (UI)

`CornerstoneApplication<AppKeystone>` owns Keystone’s lifecycle (init/load in `Initialize`, start after the framework is ready, teardown on exit). The host only bootstraps and creates the window/view shell.

```csharp
// Program.cs (Desktop / Browser / etc.)
AppBootstrap.Initialize("MyApp", typeof(Program).Assembly, args);
BuildAvaloniaApp().UseCornerstone(args).StartWithClassicDesktopLifetime(args);

// App : CornerstoneApplication<AppKeystone>
//   RegisterServices()
//     base.RegisterServices()  → ensure AppBootstrap, register IDispatcher
//     Register Keystone, channels, processors, AppViewModel on AppBootstrap.DependencyProvider
//   Initialize()               → infrastructure Init/Load + Keystone Init/Load
//   OnFrameworkInitializationCompleted()
//     desktop.MainWindow = new AppWindow(Keystone.ViewModel);  // shell only
//     base… → StartOwnedLifecycles (Keystone Start, then infrastructure Start)
```

See [CornerstoneApplication.md](CornerstoneApplication.md) and [AppBootstrap.md](AppBootstrap.md) for the full sequence.

### Console / Headless

ViewModels are not required (no UI dispatching). Drive Keystone yourself after bootstrap:

```csharp
AppBootstrap.Initialize("MyApp", typeof(Program).Assembly, args);

var services = AppBootstrap.DependencyProvider;
// Register AppState, AppBus, AppEngine, AppKeystone, …
services.AddSingleton<AppKeystone>();

AppBootstrap.InitializeInfrastructure();

var keystone = AppBootstrap.GetInstance<AppKeystone>();
keystone.InitializeLifecycle();
keystone.LoadLifecycle();
keystone.StartLifecycle();
AppBootstrap.StartInfrastructure();

var bus = keystone.Bus;
// bus.Subscribe<…>(…);

var quit = new ManualResetEventSlim(false);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; quit.Set(); };

while (!quit.IsSet)
{
    if (keystone.CanProcessLifecycle())
    {
        keystone.ProcessLifecycle();
    }
}

AppBootstrap.TeardownLifecycle(keystone);
AppBootstrap.ShutdownInfrastructure();
```

---

## Related documentation

| Document | Relationship |
|----------|----------------|
| [KeystoneFeatureTab.md](KeystoneFeatureTab.md) | How-to: dockable feature tab with Keystone + AppDispatcher |
| [AppDispatcher.md](AppDispatcher.md) | Optional UI projection loop over State |
| [Lifecycle.md](Lifecycle.md) | Track / Release and phase order |
| [CornerstoneApplication.md](CornerstoneApplication.md) | Avalonia host lifecycle |
| [AppBootstrap.md](AppBootstrap.md) | Process DI and infrastructure |
| [Controls/DockingLifecycle.md](Controls/DockingLifecycle.md) | Tab Activate/Deactivate and dispatcher Track/Release |