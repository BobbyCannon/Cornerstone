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