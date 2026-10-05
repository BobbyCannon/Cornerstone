# XAML compilation

Markup is compiled at **build**, not parsed from XML at startup (this tree does not ship a runtime XAML loader).

Two different tools touch markup:

| Tool | When | What it does |
|------|------|----------------|
| **BuildTasks** | MSBuild (`UsingTask`) | Compiles `.cxaml` / `.axaml` to IL and packs resources |
| **Generators** | Roslyn | `x:Name` / `InitializeComponent` stubs and generated styled properties |

BuildTasks cannot load `Cornerstone.Presentation.dll` (net10 + platforms). It is a `netstandard2.0` task assembly MSBuild hosts separately.

---

## BuildTasks

Two tasks:

**`CompileCornerstoneXamlTask`** turns `CornerstoneXaml` items into IL in the consumer assembly: control trees, styles, templates, markup extensions. **Compiled bindings** are one output of that pass, not the whole job. They are off unless `CornerstoneUseCompiledBindingsByDefault` is true.

**`GenerateCornerstoneResourcesTask`** packs `CornerstoneResource` (fonts, images, extra XAML) and an index the runtime asset loader uses. Packed assets load with `csres://` URIs (`ResourceInclude`, fonts). The compiler does not accept `avares://`. XAML diagnostics use `CSDC*` codes (for example `CSDC2000`).

Repo `Directory.Build.targets` infers when to import the tasks (XAML items, Presentation/Theme/Diagnostics references, WinExe, android/ios/browser). Tooling projects are skipped. Apps do not set an opt-in flag.

Use `CornerstoneXaml` for `*.cxaml`. If you also include `*.xaml`, keep that as a **separate** item — a `*.xaml` glob also matches `.axaml` / `.cxaml`.

---

## Shared sources in BuildTasks

A handful of Presentation files (`Point`, `Color`, `Thickness`, parsers, …) are **linked** into the tasks project so the compiler can parse values like `"#FF00AA"` without referencing the runtime DLL.

`BUILDTASK` is defined only on that project. Public runtime types use `#if !BUILDTASK public` so they stay **internal** in the tasks DLL (same namespace, two assemblies). Types that are already `internal` need no `#if`. Runtime-only usings (for example animators) are also gated.

Apps never compile against BuildTasks; they load it as an MSBuild task.

---

## Build notes

Do not full-solution-build while changing Presentation. Build Presentation per TFM with `--no-dependencies`; consumers with `-p:BuildProjectReferences=false`.

BuildTasks output is `tools\$(Configuration)\netstandard2.0\`. Consumers shadow-copy it under `obj\` and use `TaskHostFactory` so Visual Studio Rebuild can replace the DLL.
