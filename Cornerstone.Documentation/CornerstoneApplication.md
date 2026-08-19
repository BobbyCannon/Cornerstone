# Cornerstone Application

Avalonia application shell that wires **[AppBootstrap](AppBootstrap.md)**, infrastructure lifecycle, and optional **[Keystone](Keystone.md)** start/stop.

---

## Types

| Type | Role |
|------|------|
| `CornerstoneApplication` | Base Avalonia `Application`: ensure bootstrap, UI dispatcher, infrastructure init/start/shutdown, crash logging |
| `CornerstoneApplication<T>` where `T : ILifecycle` | Resolves `T` (usually `AppKeystone`) from DI; init/load/start/teardown with the app |

---

## Appearance

Hosts typically install `<CornerstoneTheme ThemeColor="…" ThemeMode="…" />` in application styles, then override from settings after load (color, mode, and UI density). See [Themes.md](Themes.md).

---

## Keystone shape (reminder)

```
AppKeystone
    ├── AppState
    ├── AppBus
    |   └── Channels
    ├── AppEngine
    |    └── Processors
    └── lifecycle phases (via LifecycleTracker)

AppKeystone with ViewModel : AppKeystone
    └── ViewModel { get; }   // tracked child
```

---

## Startup sequence

```
Host Main
  AppBootstrap.Initialize(name, assembly, args)
  AppBuilder.Configure<App>().UseCornerstone(args).Start…

Avalonia RegisterServices
  EnsureAppBootstrapForAvalonia()     // design-time safe
  SetSingleton IDispatcher + ClipboardService
  App.RegisterServices(provider)      // Keystone, VMs, features

Application.Initialize
  serializer config, base.Initialize
  InitializeInfrastructure()          // RuntimeInformation + IPlatform Init/Load
  (generic) Keystone = GetInstance<T>()
  (generic) Keystone.InitializeLifecycle / LoadLifecycle

OnFrameworkInitializationCompleted
  App creates MainWindow / MainView from Keystone.ViewModel
  base → StartOwnedLifecycles
    (generic) Keystone.StartLifecycle()
    (generic) ProcessLifecycle timer (off UI thread, 50 ms)
    StartInfrastructure()             // RuntimeInformation + Platform Start

Shutdown (Exit / controlled lifetime)
  TeardownLifecycle(Keystone)
  ShutdownInfrastructure()
```

Unhandled exceptions are forwarded to `AppBootstrap.LogException` (AppDomain, UI dispatcher, unobserved tasks).

---

## Avalonia app pattern

```csharp
// Program.cs
AppBootstrap.Initialize("MyApp", typeof(Program).Assembly, args);
BuildAvaloniaApp().UseCornerstone(args).StartWithClassicDesktopLifetime(args);

// App.axaml.cs
public class App : CornerstoneApplication<AppKeystone>
{
    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new AppWindow(Keystone.ViewModel);
                break;
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new AppView(Keystone.ViewModel);
                break;
        }
        base.OnFrameworkInitializationCompleted(); // starts Keystone + infrastructure
    }

    public override void RegisterServices()
    {
        base.RegisterServices();
        RegisterServices(AppBootstrap.DependencyProvider, Design.IsDesignMode);
    }

    public static void RegisterServices(DependencyProvider dependencyProvider, bool designOrUnitTesting)
    {
        if (designOrUnitTesting)
        {
            dependencyProvider.AddDesignStubs();
        }

        dependencyProvider.AddSingleton<AppState>();
        dependencyProvider.AddSingleton<AppBus>();
        dependencyProvider.AddSingleton<AppEngine>();
        dependencyProvider.AddSingleton<AppKeystone>();
        dependencyProvider.AddSingleton<AppViewModel>();
        // …
    }
}
```

The Avalonia side only creates the **window/view shell**. Domain setup and lifecycle for Keystone are owned by `CornerstoneApplication<T>` + registrations on `AppBootstrap.DependencyProvider`.

---

## View / ViewModel / State (example: GrokMonitor)

Worked host: `Cornerstone.GrokMonitor`. Use the same split when adding Keystone + AppDispatcher apps.

| Layer | Owns | Does not |
|-------|------|----------|
| `*State` | Domain snapshot (`SpeedyList`, `CornerstoneObject`) | Avalonia, selection, bindings |
| Processor | Mutate State; publish follow-up messages | Direct UI |
| `*TabView` / `*TabViewModel` | Layout, selection, publish intent, Track* wiring | Reading State after Track* is configured |
| Child ViewModels | Presentation rows / combo items | Being bound as State types |

**Rules**

- UI binds **ViewModels only**. Do not bind `*State` types in XAML.
- Keystone (Bus, State, processors) is **business logic only** and runs **off the UI dispatcher**. Nothing in Keystone should call `IDispatcher.Dispatch` or assume Avalonia.
- AppDispatcher exists to keep ViewModels in sync with State for **visual representation and/or user input**. Apply runs on the UI thread; it is not a place for domain rules.
- A View/ViewModel may mention State **only when configuring** `TrackProperties` / `TrackCollection` / `TrackBinding` / `TrackIngress` (and design-time / host composition). After that, apply and commands use VM properties and bus publish.
- Same-type lists: `TrackCollection(source, dest, comparer?, mode?)`. State row → row ViewModel: `TrackCollection(source, dest, same, create, update, remove)` (GrokUsage sessions / periods). `TrackBinding` is for charts / custom multi-sink apply, not a list factory. Do not keep `_state.FindById` in `ApplyModelChanges`.
- Host `AppViewModel` may hold `AppState` to create tabs and apply theme. Feature tabs should not keep using State after Track*.
- **Exception:** editor-style controls (`TextEditor` / `TextEditorViewModel`) may hold most document state in the ViewModel. That is a control architecture limit — do not copy it onto feature dashboards. See [Agent/TextEditor.md](Agent/TextEditor.md).

**Naming**

- Domain types: `*State` (`GrokHomeUsageState`). Settings files stay `AppSettings`.
- Presentation: `*ViewModel` (`GrokUsageTabViewModel`, `GrokSessionRowViewModel`, `GrokUsagePeriodViewModel`).
- Shared get-only contracts (`IGrokHomeUsage`) exist so `TrackProperties<TContract>` maps one-way. They are not bind targets.

**Folders**

Keep each tab **View + ViewModel side by side**. Put one-off child ViewModels in `ViewModels/` (same idea as `Models/` for reader DTOs):

```
FeatureName/
  FeatureTabView.axaml
  FeatureTabView.axaml.cs
  FeatureTabViewModel.cs
  ViewModels/          # row / combo / nested VMs
  Models/              # non-State DTOs
  State/  Channels/  Processors/
```

Namespaces follow folders.

See [Keystone.md](Keystone.md), [KeystoneFeatureTab.md](KeystoneFeatureTab.md), and [AppDispatcher.md](AppDispatcher.md).

---

## Console / Headless

No `CornerstoneApplication` required. Use AppBootstrap + manual Keystone lifecycle (and optional process loop) as shown in [Keystone.md](Keystone.md#console--headless).

---

## Design-time

If Avalonia constructs the app without host `Main`, `RegisterServices` / `Initialize` call `EnsureInitialized` so `GetInstance` and control default constructors still work. Prefer calling `AppBootstrap.Initialize` from every real host entry point.

---

## See also

- [AppBootstrap.md](AppBootstrap.md) — process bootstrap and infrastructure helpers  
- [Keystone.md](Keystone.md) — Bus : State : Engine  
- [Lifecycle.md](Lifecycle.md) — phase rules  
- [ViewIntegration.md](ViewIntegration.md) / [AppDispatcher.md](AppDispatcher.md) — UI projection