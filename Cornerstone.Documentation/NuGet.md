# NuGet packages

The public release is seven packages.

## Packages

| Package | What a consumer gets |
|---------|----------------------|
| `Cornerstone` | Core library: bootstrap, Keystone, lifecycle, storage, and sync. The Roslyn analyzer ships inside this package at `analyzers/dotnet/cs/Cornerstone.Generators.dll` (source reflection, commands, and the `.cxaml` name and property generators). |
| `Cornerstone.Presentation` | UI runtime for `net10.0`, Android, iOS, browser, and Windows. Includes the `.cxaml` compiler under `tools/netstandard2.0`, build props and targets, the macOS native library, and `Cornerstone.Presentation.Remote.Protocol.dll` in each `lib` folder. |
| `Cornerstone.Templates` | `dotnet new` templates `cornerstone.basic`, `cornerstone.app`, and `cornerstone.all`. No assemblies. The Visual Studio extension uses this package too. |
| `Cornerstone.EntityFramework` | Entity Framework integration for Cornerstone storage. |
| `Cornerstone.PowerShell` | PowerShell host integration. |
| `Cornerstone.Automation` | Windows UI Automation. |
| `Cornerstone.Automation.Presentation` | In-process WebView automation. Windows only. |

An app that compiles `.cxaml` needs a direct package reference to both `Cornerstone` and `Cornerstone.Presentation`. NuGet does not flow analyzers through Presentation, so a Presentation-only reference compiles markup tasks and does not generate `InitializeComponent`.

## Inside Presentation, not separate packages

These projects stay in the repo. Their output is either copied into `Cornerstone.Presentation` or used only from source.

| Project | How it ships |
|---------|----------------|
| `Cornerstone.Presentation.Remote.Protocol` | `netstandard2.0` designer contract. Presentation compiles against the project. The DLL is packed into Presentation. There is no protocol package. |
| `Cornerstone.Presentation.BuildTasks` | `.cxaml` compiler, packed under `tools/netstandard2.0`. |
| `Cornerstone.Designer.HostApp` | Optional designer exe under `tools/net10.0/designer` when that build exists. |
| `Cornerstone.Presentation.DevGenerators` | Generators that build Presentation itself. Not copied into any package. |
| `Cornerstone.Presentation.Diagnostics` | DevTools. Apps that want them project-reference this project. |
| `Cornerstone.Presentation.Sandbox` | Unstyled desktop smoke-test exe. |
| `Cornerstone.Presentation.Remote.Wpf` and `Remote.Wpf.Host` | WPF preview harness. The Visual Studio extension project-references them. |

`Cornerstone.Generators` is not a package. Its DLL is copied into the `Cornerstone` package. There is no `Cornerstone.Presentation.Theme` package and no `Cornerstone.Presentation.Native` package. Theme chrome and `runtimes/osx/native/libCornerstoneNative.dylib` are already inside Presentation.

## Source only

`Cornerstone.Vlc`, `Cornerstone.Mapsui`, and `Cornerstone.Esri` are not packages. An app project-references the project and builds it from this tree. Each SDK keeps its own license and native runtime.

## Not this release

`Cornerstone.Avalonia` is a retired package id. Do not reference it. Do not load that retired UI package and `Cornerstone.Presentation` in the same process.

Samples, tests, and apps do not pack.
