# AppBootstrap

`AppBootstrap` is the **host-agnostic process bootstrap** for Cornerstone.  
Call it once from the host entry point (console, service, website, Avalonia desktop/browser/mobile, etc.) **before** resolving services or starting application lifecycles.

It lives in `Cornerstone.Runtime` and is independent of Avalonia or any other UI stack.

---

## Responsibilities

| Concern | Owned by AppBootstrap? |
|---------|------------------------|
| Process-wide DI root (`DependencyProvider`) | Yes |
| Application name, assembly, CLI args | Yes |
| `RuntimeInformation` creation + early setup | Yes |
| Platform registration (`Platform.Initialize`) | Yes |
| Infrastructure lifecycle (RuntimeInformation + `IPlatform`) | Yes (helpers) |
| Keystone / app domain lifecycle | **No** — app or `CornerstoneApplication` owns that |
| UI dispatcher loop / AppDispatcher | **No** — presentation layer |
| Crash log under application data | Best-effort helper |

---

## Core API

### Process bootstrap

```csharp
// Host Main / platform entry — once per process
AppBootstrap.Initialize(
    applicationName: "MyApp",
    applicationAssembly: typeof(Program).Assembly,
    args: args,
    dispatcher: null,           // optional; Avalonia registers later
    providerName: null          // optional DependencyProvider name
);

// Design-time / tests that never hit host Main
AppBootstrap.EnsureInitialized(
    applicationName: "Design",
    applicationAssembly: null,  // defaults to calling assembly
    args: null,
    dispatcher: null
);
```

| Method | Behavior |
|--------|----------|
| `Initialize` | Creates args, `DependencyProvider`, `RuntimeInformation`; runs `SetupCornerstoneDependencies`; calls `Platform.Initialize`. **Throws** if already initialized. |
| `EnsureInitialized` | No-op if already initialized; otherwise calls `Initialize` (for designer / unit scenarios). |
| `IsInitialized` | Whether process bootstrap completed. |
| `Reset` | Clears static state. **Unit tests only.** |
| `RegisterAsTests` | Points bootstrap at a `CornerstoneTest` dependency provider. **Tests only.** |

### Service resolution

```csharp
var runtime = AppBootstrap.GetInstance<IRuntimeInformation>();
var keystone = AppBootstrap.GetInstance<AppKeystone>();
```

Requires `Initialize` (or successful `EnsureInitialized`).

**Policy (important):** Feature code, view models, commands, and most framework libraries **must not** call `AppBootstrap` statics (`GetInstance`, `DependencyProvider`, `DateTimeProvider`, …). Pass dependencies via constructor or method parameters. Static resolution is a host/design-time escape hatch only — and we intend to **fence** it so casual use is blocked (see [Todo/AppBootstrapFence.md](Todo/AppBootstrapFence.md) and `.grok/rules/framework-primitives.md`).

`GetInstance` remains useful only for:

- Host entry after bootstrap  
- Avalonia/XAML default constructors (design-time)  
- Existing host shell types that already own bootstrap (do not add new call sites)

### Infrastructure lifecycle

These drive **`RuntimeInformation`** and the registered **`IPlatform`** (not Keystone):

```csharp
AppBootstrap.InitializeInfrastructure();  // Init + Load if needed (idempotent)
AppBootstrap.StartInfrastructure();       // Start RuntimeInformation then Platform
AppBootstrap.ShutdownInfrastructure();    // Stop/Unload/Uninitialize Platform then RuntimeInformation
```

`TeardownLifecycle(ILifecycle)` applies stop → unload → uninitialize for any lifecycle (used for Keystone on Avalonia shutdown).

### Other

| Member | Role |
|--------|------|
| `ApplicationArguments` | Parsed host args (if provided) |
| `DependencyProvider` | Process DI root |
| `RuntimeInformation` | App paths, platform info, etc. |
| `DateTimeProvider` | Set at initialize (real time) |
| `TryGetPlatform` | Resolve `IPlatform` if registered |
| `LogException` | Best-effort crash log under `ApplicationDataLocation/CrashLogs` |

---

## What `Initialize` wires

1. `ApplicationArguments` (optionally `Parse(args)`)
2. `DateTimeProvider.RealTime`
3. `DependencyProvider` named `"Cornerstone"` (or `providerName`)
4. `RuntimeInformation` — application name override + `Initialize(assembly)`
5. Singleton registration of args + `SetupCornerstoneDependencies` (date/time, runtime info, optional dispatcher, provider itself)
6. `Platform.Initialize(DependencyProvider, RuntimeInformation)`

After this, app code registers Keystone, view models, and feature services on `AppBootstrap.DependencyProvider`.

---

## Lifecycle order (Avalonia + Keystone)

Typical desktop/browser/mobile sample:

```
Host Main
  AppBootstrap.Initialize(name, assembly, args)
  AppBuilder…StartWithClassicDesktopLifetime (or platform equivalent)

Avalonia Application.RegisterServices
  EnsureAppBootstrap (design-time safe)
  Register IDispatcher / ClipboardService
  App.RegisterServices(provider)  → Keystone, VMs, channels, …

Application.Initialize
  InitializeInfrastructure()      → RuntimeInformation + Platform Init/Load
  Keystone.InitializeLifecycle()
  Keystone.LoadLifecycle()

OnFrameworkInitializationCompleted
  Create MainWindow / MainView from keystone.ViewModel
  StartOwnedLifecycles:
    Keystone.StartLifecycle()
    StartInfrastructure()         → RuntimeInformation + Platform Start

Shutdown
  TeardownLifecycle(Keystone)     → Stop / Unload / Uninitialize
  ShutdownInfrastructure()
```

Console / headless hosts call the same `AppBootstrap` APIs, then drive Keystone lifecycle and an optional process loop themselves (see [Keystone.md](Keystone.md)).

Details of the Avalonia shell: [CornerstoneApplication.md](CornerstoneApplication.md).  
Lifecycle phase rules: [Lifecycle.md](Lifecycle.md).

---

## Desktop host example

```csharp
[STAThread]
public static void Main(string[] args)
{
    AppBootstrap.Initialize("Cornerstone.Sample", typeof(Program).Assembly, args);
    BuildAvaloniaApp().UseCornerstone(args).StartWithClassicDesktopLifetime(args);
}
```

```csharp
public class App : CornerstoneApplication<AppKeystone>
{
    public override void RegisterServices()
    {
        base.RegisterServices(); // dispatcher + ensure bootstrap
        RegisterServices(AppBootstrap.DependencyProvider, Design.IsDesignMode);
    }

    public static void RegisterServices(DependencyProvider dependencyProvider, bool designOrUnitTesting)
    {
        dependencyProvider.AddSingleton<AppState>();
        dependencyProvider.AddSingleton<AppBus>();
        dependencyProvider.AddSingleton<AppEngine>();
        dependencyProvider.AddSingleton<AppKeystone>();
        dependencyProvider.AddSingleton<AppViewModel>();
        // …
    }
}
```

---

## Design-time and default constructors

Many Avalonia controls and sample tabs use parameterless constructors that resolve dependencies via `AppBootstrap.GetInstance<T>()`. That works because:

1. Host `Main` already called `Initialize`, or  
2. `CornerstoneApplication` called `EnsureInitialized` during `RegisterServices` / `Initialize`

Prefer **constructor injection** for objects created by `DependencyProvider`. Use `GetInstance` for XAML/design paths where Avalonia constructs the type without DI.

---

## Testing

```csharp
[TestCleanup]
public void Cleanup() => AppBootstrap.Reset();

[TestMethod]
public void MyTest()
{
    AppBootstrap.Initialize("MyTests", typeof(SomeType).Assembly);
    // …
}
```

Or attach an existing test provider:

```csharp
AppBootstrap.RegisterAsTests(cornerstoneTest);
```

- Always `Reset` between tests that touch bootstrap (static process state).  
- `GetInstance` before initialize throws `CornerstoneException`.  
- Second `Initialize` throws; use `EnsureInitialized` when “already done” is acceptable.

---

## Non-goals

- **Not** a substitute for Keystone (domain Bus / State / Engine).  
- **Not** the UI thread dispatcher or AppDispatcher tick loop.  
- **Not** multi-app-in-process isolation — one bootstrap per process (by design).  

---

## See also

| Document | Topic |
|----------|--------|
| [Keystone.md](Keystone.md) | Bus : State : Engine and hosting patterns |
| [Lifecycle.md](Lifecycle.md) | Lifecycle phases and `LifecycleTracker` |
| [CornerstoneApplication.md](CornerstoneApplication.md) | Avalonia application shell |
| [ViewIntegration.md](ViewIntegration.md) | State → ViewModel projection |
| [AppDispatcher.md](AppDispatcher.md) | Optional adaptive UI projection loop (idle/active) |