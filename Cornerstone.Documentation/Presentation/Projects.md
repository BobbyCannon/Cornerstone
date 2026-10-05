# Presentation projects

The UI stack is several projects on purpose: the **runtime** apps execute, the **theme** they look like, **build tools** that compile markup, and a **designer process** Visual Studio talks to. Win32, Skia, Android, iOS, Browser, Headless, X11, Wayland, and FreeDesktop already live in one runtime DLL.

An app that draws a window typically references Theme. Theme pulls in Presentation. Markup compile and `x:Name` generation attach at build. DevTools and the XAML previewer are optional and stay off the default app graph.

```
App (Sample, Editor, …)
  └─ Cornerstone.Presentation.Theme     chrome + product controls
       └─ Cornerstone.Presentation      runtime (windows, layout, input, drawing)
            └─ Remote.Protocol          designer message types (also used by the previewer)

Remote.Wpf (VS / WPF harness only)  →  Remote.Protocol
Apps do not reference Remote.Wpf.

Build (MSBuild / Roslyn, not in the app output)
  ├─ BuildTasks                         compile .cxaml, pack csres:// resources
  ├─ Generators                         x:Name / InitializeComponent for app code
  └─ DevGenerators                      generators for the runtime project itself

Designer (separate process)
  HostApp.exe  →  Protocol  ↔  Visual Studio

Desktop --remote
  Sample.Desktop.exe  →  Protocol  ↔  Remote.Wpf.Host (or a VSIX RemoteView)
```

Control pages stay under [Controls/](../Controls/Readme.md). Color, mode, and density stay in [Themes.md](../Themes.md). How markup is compiled stays in [XamlCompilation.md](XamlCompilation.md).

---

## Runtime and chrome

| Project | What it is |
|---------|------------|
| **Cornerstone.Presentation** | The UI engine: windows, layout, input, styling, Skia drawing, and OS backends. Targets `net10.0` plus android, ios, and browser. This is the assembly apps actually run. |
| **Cornerstone.Presentation.Theme** | Default look (`CornerstoneTheme`) and product controls (docking, TreeDataGrid, WebView, MarkdownView, …). Extra `net10.0-windows` target for WebView2. |
| **Cornerstone.Presentation.Remote.Protocol** | Small `netstandard2.0` contract for previewer messages (TCP BSON, keys, viewport frames). The Visual Studio extension can share this without loading the UI stack. `Key` / `PhysicalKey` also compile into Presentation under `Cornerstone.Presentation.Input`. |
| **Cornerstone.Presentation.Remote.Wpf** | WPF client for that protocol (`net472` and `net10.0-windows`). `RemoteSession` + `RemoteView` blit frames and forward pointer input. Visual Studio and a standalone WPF harness share this assembly. It does not reference Presentation or VSSDK. |
| **Cornerstone.Presentation.Remote.Wpf.Host** | net472 WPF exe for fast F5 of that client. Starts Sample.Desktop with `--remote tcp-bson://…`. Not packed. |

Theme stays separate so a host can boot the runtime unstyled (Sandbox, some tests) and so Windows-only native controls do not force a windows TFM onto the core runtime.

Protocol stays `netstandard2.0` so the designer and a future VSIX can speak the same messages without referencing Presentation.

---

## Build tooling

These projects are not app references. MSBuild and Roslyn load them while compiling.

| Project | What it is |
|---------|------------|
| **Cornerstone.Presentation.BuildTasks** | MSBuild tasks: compile `.cxaml` to IL and pack `CornerstoneResource` into `csres://` assets. `netstandard2.0` because the MSBuild host cannot load the multi-targeted runtime DLL. Output is `tools/$(Configuration)/netstandard2.0/`, not `bin\`. |
| **Cornerstone.Presentation.Generators** | Roslyn analyzer for consumer apps: typed `x:Name` fields and `InitializeComponent`. |
| **Cornerstone.Presentation.DevGenerators** | Roslyn analyzer **for Presentation itself** (composition, platform glue). Apps do not reference it. |

BuildTasks are embedded in the `Cornerstone.Presentation` package (`tools/`). The name and property generators ship in the `Cornerstone` package (`analyzers/dotnet/cs/Cornerstone.Generators.dll`), not in Presentation. Neither is its own package. DevGenerators stays in the repo only. The package list is [NuGet.md](../NuGet.md).

DevGenerators is a second Roslyn analyzer, not a different compiler host. It stays separate until its ungated triggers are opt-in. Follow-up: [../Todo/SingleRoslynGenerator.md](../Todo/SingleRoslynGenerator.md). Do not fold BuildTasks into `Cornerstone.Generators`; MSBuild loads that assembly, Roslyn does not.

---

## Designer

The XAML previewer is a **child process**, not something apps link.

**Cornerstone.Designer.HostApp** is the exe Visual Studio launches. It binds the runtime XAML loader, loads markup in design mode (`DesignWindowLoader`), and talks to the IDE (`RemoteDesignerEntryPoint` — TCP BSON, win32 parent, or HTML). Types live in namespace `Cornerstone.Presentation.DesignerSupport` inside this exe. Reflection-heavy; not for shipped apps.

HostApp starts with the **previewed app’s** `deps.json` so it uses that app’s Presentation, not a private copy. Product apps do not reference HostApp.

The Presentation nupkg copies `Cornerstone.Designer.HostApp.dll` under `tools/net10.0/designer/`. In this repo the designer uses project output.

---

## Optional and repo-only hosts

| Project | What it is |
|---------|------------|
| **Cornerstone.Presentation.Diagnostics** | In-app DevTools (`AttachDevTools`): visual tree, properties, events. Desktop product apps project-reference it and attach after setup in DEBUG. Not packed, so it does not ship in every themed app. Different from the Keystone bus / Profiler panel in [Diagnostics.md](../Diagnostics.md). |
| **Cornerstone.Presentation.Sandbox** | Tiny desktop **exe**: one unstyled window on Presentation only (no Theme). Proves Win32 + Skia + HarfBuzz boot without chrome. Not a library and not packed. |

Related but named outside Presentation*: `Cornerstone.Automation.Presentation` automates an in-process WebView on top of Theme (Windows). It is a NuGet of its own.

---

## NuGet

What publishes, what is embedded, and what stays a project reference is [NuGet.md](../NuGet.md).

`Cornerstone.Presentation` is the only Presentation-family package. It carries the runtime, the `.cxaml` build tasks, build props and targets, and `Cornerstone.Presentation.Remote.Protocol.dll`. There is no Theme package and no protocol package. BuildTasks, DevGenerators, HostApp (except the optional designer DLL under `tools/`), Diagnostics, Sandbox, Remote.Wpf, and Remote.Wpf.Host are not packages.

---

## Why they stay separate

| Split | Reason |
|-------|--------|
| Theme vs Presentation | Unstyled hosts; Windows-only native controls on Theme |
| Protocol vs Presentation | `netstandard2.0` project so the IDE can compile the contract without the UI stack. The DLL ships inside the Presentation package. |
| Remote.Wpf vs Protocol | WPF blit/input; Protocol stays usable from non-WPF hosts |
| BuildTasks vs Presentation | MSBuild cannot load the runtime DLL |
| Generators vs DevGenerators | Consumer `x:Name` vs generators that build the runtime |
| HostApp vs Presentation | Previewer process; trimming-hostile; unused by apps |
| Diagnostics vs Theme | DevTools must be opt-in |
| Sandbox | App, not a library |
