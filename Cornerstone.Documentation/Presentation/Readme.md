# Presentation

`Cornerstone.Presentation` is the UI runtime: windows, layout, input, styling, and drawing. Apps build on it with `Cornerstone.Presentation.Theme` (the default theme and product controls) and optional Diagnostics.

This folder is the **runtime**. Control pages stay under [Controls/](../Controls/Readme.md). Color, mode, and density stay in [Themes.md](../Themes.md).

---

## Index

| Document | Summary |
|----------|---------|
| [Projects.md](Projects.md) | Each Presentation* project: role, who uses it, NuGet vs repo-only |
| [../NuGet.md](../NuGet.md) | Packages that publish, and what is embedded in Presentation |
| [XamlCompilation.md](XamlCompilation.md) | Build-time XAML compile and resource packing (`BuildTasks`) |
| [Upstream.md](Upstream.md) | Third-party source notice |
| [NativeLayering.md](NativeLayering.md) | Native views under Skia; holes; Windows frame + layered composition window |

---

## Projects

Full map: [Projects.md](Projects.md). Short list:

| Project | Role |
|---------|------|
| `Cornerstone.Presentation` | Runtime (`net10.0` + android + ios + browser) |
| `Cornerstone.Presentation.Theme` | Default theme (`CornerstoneTheme`) and product controls |
| `Cornerstone.Presentation.Remote.Protocol` | Designer protocol (`netstandard2.0`) |
| `Cornerstone.Presentation.Remote.Wpf` | WPF remote surface (`net472` + `net10.0-windows`): session, frames, pointer |
| `Cornerstone.Presentation.Remote.Wpf.Host` | net472 WPF harness: F5 `RemoteView` against Sample.Desktop `--remote` |
| `Cornerstone.Presentation.BuildTasks` | MSBuild: compile `.cxaml` and pack resources |
| `Cornerstone.Presentation.Generators` | Roslyn: `x:Name`, generated properties (shipped in the `Cornerstone` package, not Presentation) |
| `Cornerstone.Presentation.DevGenerators` | Generators for the runtime project itself |
| `Cornerstone.Designer.HostApp` | Exe Visual Studio launches for the XAML previewer (design-mode load + remote protocol) |
| `Cornerstone.Presentation.Diagnostics` | Optional DevTools (`AttachDevTools`) |
| `Cornerstone.Presentation.Sandbox` | Unstyled desktop smoke-test app |

---

## When adding a Presentation page

1. One topic per file under `Presentation/`.
2. Link it from this table and from the parent [Readme.md](../Readme.md).
3. Describe host-facing behavior here. Keep implementation file maps out of these pages.
