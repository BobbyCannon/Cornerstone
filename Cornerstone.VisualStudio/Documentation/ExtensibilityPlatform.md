# Extensibility platform — VSSDK vs VisualStudio.Extensibility

Why Cornerstone stays on the classic Visual Studio SDK, and when a move to the newer out-of-process model might be worth revisiting.

**Last updated:** 2026-08-01

---

## Short answer

**A full, feature-parity move to VisualStudio.Extensibility (modern .NET, out-of-process) is not possible today.**

Cornerstone remains a **classic VSSDK** extension on **.NET Framework 4.7.2**. That path is still supported for Visual Studio 2022 and Visual Studio 2026. The new model does not yet cover deep editor IntelliSense hooks or the project-system integration this extension needs, and it still lacks a first-class multi-pane document designer.

A **partial** path exists if the product accepts a UX change: **stop injecting the preview into the `.axaml` document tab** and show the active document’s preview in a **tool window** (or an **external preview process**). That bypasses the missing document-editor factory, but it is not a drop-in port and does not unlock IntelliSense parity.

**Runtime note:** On current **Visual Studio 2026**, out-of-process VisualStudio.Extensibility extensions **can target .NET 10** (declare `DotnetTargetVersions` / build against `net10.0-windows…`). Host-managed **net8 and net10** coexist until .NET 8 LTS ends (**2026-11-10**), after which net10 becomes the primary default. The blocker for Cornerstone is **API / product surface**, not “waiting for net10 on the host.” See [.NET version reality](#net-version-reality-for-visualstudioextensibility).

### Strategic expectation: classic .NET Framework is not going away soon

**Plan as if deep IDE integration stays on classic VSSDK / .NET Framework for a long time — possibly for the life of Visual Studio as we know it.**

That is not pessimism; it matches how the product is built:

| Reality | Why it matters |
|---------|----------------|
| **`devenv` is still a .NET Framework + WPF + MEF process** | Anything that must run *inside* the IDE process (editors, many MEF parts, full project-system hooks, hybrid “in-proc Extensibility”) is still Framework-bound. |
| **Classic VSSDK remains supported** on VS 2022 and VS 2026 | Microsoft continues to ship and document the old model for extensions that need the full API surface. There is no announced “kill date” for classic VSIX. |
| **VisualStudio.Extensibility is additive, not a forced replacement** | OOP gets isolation, modern .NET, hot-load. It does **not** reimplement decades of editor/designer/project COM/MEF APIs on a schedule that deep tools can depend on. |
| **Hybrid in-proc mode exists because the gap is real** | When new APIs are missing, the escape hatch is “run in-process and call classic VSSDK” — which reintroduces **.NET Framework**. |
| **Marketplace gravity** | Thousands of extensions and Microsoft’s own tooling still assume the classic surface. Forcing a hard cutover would strand the ecosystem. |

So for Cornerstone:

1. **Do not bet the product** on “VS will drop netfx and we’ll rewrite once.”
2. **Stay excellent on classic VSSDK** for document designer, completion, and project integration.
3. **Use modern .NET where it does not require leaving the IDE process** — especially the **previewer / side process**, Core multi-targeting, tests, and optional thin OOP sidecars for new, self-contained features.
4. Treat full OOP migration as a **product choice** (accept smaller surface + different UX), not an inevitable platform upgrade.

---

## What “next platform” means

Microsoft’s newer stack is **VisualStudio.Extensibility** (sometimes called the out-of-process / OOP extensibility model). It is not “retarget the existing VSIX to `net8`.”

| | Classic VSSDK (Cornerstone today) | VisualStudio.Extensibility |
|---|-----------------------------------|----------------------------|
| Process | In-process with `devenv` | Out-of-process (isolated) |
| Runtime | **.NET Framework only** | **.NET** LTS host-managed (**net8** and **net10** on current VS 2026; declare via `DotnetTargetVersions`) |
| API surface | Full IDE (COM, MEF, editor, project system) | Growing; still incomplete for many scenarios |
| Install | Usually requires restart | Can hot-load without restart |
| If the extension crashes | Can hang or take down VS | Isolated from the IDE |

**In-proc VisualStudio.Extensibility** can call classic VSSDK APIs to fill gaps, but that path **still requires .NET Framework** because the code runs inside the VS process. It does **not** deliver a “.NET Core / net10 only” extension while keeping full designer/editor fidelity.

---

## Compatibility matrix (current shipping shape)

| Item | Value |
|------|--------|
| Extensibility model | Classic VSSDK + MEF |
| VSIX project TFM | `net472` |
| Shared logic | `Cornerstone.VisualStudio.Core` → `netstandard2.0` |
| Tests / sample apps | Often `net10.0` (host-agnostic) |
| VS install range | Community **`[17.0, 19.0)`** (see `source.extension.vsixmanifest`) |
| Architectures | **amd64**, **arm64** |
| VS 2022 | Supported (17.0+) |
| VS 2026 | Supported (18.x; range upper bound is 19.0 exclusive) |
| Prerequisites | Core Editor, .NET Core development tools, Roslyn language services |

Microsoft’s VS 2026 guidance: most extensions built for minimum 17.0+ continue to work; classic VSIX is not retired for this class of tooling.

---

## Why not VisualStudio.Extensibility yet

Cornerstone’s value is deep IDE integration, not a single command or tool window. Mapping current features to the new model:

| Feature area | Current implementation | OOP VisualStudio.Extensibility today | Verdict |
|--------------|------------------------|--------------------------------------|---------|
| Custom Avalonia XAML editor | `IVsEditorFactory`, `ProvideEditorExtension`, `ProvideXmlEditorChooserDesignerView` for `.axaml` / Avalonia `.xaml` | No equivalent multi-pane document editor factory | **Blocker for parity**; [bypassable](#alternative-preview-outside-the-document) if the product drops the custom document |
| Split designer pane | Hosts full `IVsCodeWindow` + WPF preview (`AvaloniaDesigner`, `VsCodeWindowHost`, `EditorPane`) | Tool windows exist; not a first-class document designer that owns the open file | **Blocker for parity**; tool window / external window is a UX rewrite, not a port |
| Live preview host | `PreviewerProcess` + Avalonia.Remote.Protocol + WPF `WriteableBitmap` surface | Remote UI is a poor fit for live frames + pointer input (see below) | **Hard rewrite**; prefer **process-owned** preview UI over pure Remote UI |
| IntelliSense completion | MEF `ICompletionSource` + `IOleCommandTarget` | No MEF completion OOP; closest path is an **LSP** language server | **Hard rewrite** (unchanged by tool-window pivot) |
| Paste / typing manipulators | Command filter + text view listeners | Different model (editor edits / LSP); not a port | **Hard rewrite** |
| Error squiggles + Error List | `ITagger` + `ITableManagerProvider` | Limited tagger / diagnostics support; different Error List integration | **Partial** |
| Suggested actions (light bulbs) | `ISuggestedActionsSource` | No direct parity; LSP code actions possible | **Rewrite** |
| Options / settings | `ProvideOptionPage` + `ShellSettingsManager` | Settings API exists | **Feasible** |
| Output window logging | `IVsOutputWindow` | Output window API exists | **Feasible** |
| Project / outputs / assemblies | DTE, CPS MEF, `IVsBuildPropertyStorage`, MSBuild properties | Project Query covers a subset of scenarios | **Partial** |
| Snippets / project templates | `.pkgdef` + template nupkg packaging | Different contribution model | **Partial** |
| CPS project component | `IProjectDynamicLoadComponent` | Not available the same way out of process | **Stay on VSSDK** |

Settings and output-window support alone are not enough to ship the product. Even with a tool-window preview, **editor completion** remains a hard stop for full OOP parity. The custom **designer document** is only optional if the product accepts non-document preview UX.

---

## Alternative: preview outside the document

### Idea

Instead of injecting the previewer into the `.axaml` tab (`EditorPane` → `AvaloniaDesigner` split with `IVsCodeWindow`), use the **default editor** for source and show the **selected / active document** in a **tool window** (or an external window).

That is an intentional product change:

| Gain | Cost |
|------|------|
| No need for multi-pane `IVsEditorFactory` / designer logical views | Lose in-tab split Source/Design experience |
| Dockable, single-instance preview (like many other tooling windows) | Users must open/keep a separate pane |
| OOP *shell* becomes more plausible for preview chrome | Live interactive fidelity still needs a real bitmap + input surface |

### How Cornerstone works today (document-hosted)

1. `IVsEditorFactory` opens a custom pane (`EditorPane`).
2. `AvaloniaDesigner` hosts source (`IVsCodeWindow` via `VsCodeWindowHost`) and preview (`AvaloniaPreviewer`).
3. `PreviewerProcess` runs the Avalonia remote host, receives frames, and updates a WPF `WriteableBitmap` (~60 FPS UI notify throttle; inactive tabs suspend their host).
4. Pointer/keyboard on the WPF surface is forwarded back over Avalonia.Remote.Protocol.

Only the **IDE hosting surface** is VSSDK-specific; the remote protocol and process model are already out-of-process.

### Three ways to host “preview outside the tab”

| Option | Runtime | Live interactive preview? | Notes |
|--------|---------|---------------------------|--------|
| **A. Classic VSSDK tool window** | Still `net472` | **Yes** — same WPF + `WriteableBitmap` stack | Validates UX without changing platform. No net10 in the VSIX host. |
| **B. OOP tool window (Remote UI)** | Host-managed .NET (net8 today) | **Poor fit** | Tool windows and `VisibleWhen` (e.g. active `\.axaml$`) exist, but Remote UI is not full WPF. |
| **C. External / process-owned preview window** | **net10 process possible today** | **Yes** — native Avalonia UI in the host process | Best path if the goal is modern .NET libraries for preview. VS extension stays thin (launch, document path, build outputs). |

**Recommended if chasing modern .NET:** option **C** (or A for UX-only), not pure Remote UI for the frame surface.

### Why pure Remote UI struggles with live preview

Remote UI (OOP tool window content) is designed for declarative WPF **DataTemplates** bound to a serializable data context:

- No custom controls from the extension assembly on the IDE surface
- No code-behind / ordinary event handlers (commands + data binding)
- Serializable data only (`DataContract`, primitives, collections, monikers, static images)
- Image support = **VS catalog monikers + packaged static assets**, not a live `WriteableBitmap` stream

Pushing designer frames through Remote UI (e.g. re-encoding PNG/base64 every frame) would be slow, awkward, and still a bad model for hit-testing and input. That is not a port of `AvaloniaPreviewer`.

### What a tool-window pivot does *not* unlock

Moving preview out of the document **does not** migrate:

- MEF completion, command filters, paste manipulators → still classic VSSDK or a full **LSP** rewrite
- Full project-system / CPS parity
- Automatic “net10-only” for the IDE glue that still must talk to VS

A realistic hybrid remains: **classic VSSDK (or thin OOP shell) for IDE integration + net10 process for preview/heavy logic**.

---

## .NET version reality for VisualStudio.Extensibility

### Host-managed runtime (still not “any TFM you want”)

OOP extensions run in a **Visual Studio extension host** that ships supported **.NET LTS** runtimes. You still declare known-good targets via `DotnetTargetVersions` on `ExtensionConfiguration` / metadata; VS picks an installed host runtime. Default is the **oldest still-supported LTS** unless you declare otherwise. F5 can select which host runtime to debug against.

| Claim | Reality (as of 2026-08-01) |
|-------|----------------------------|
| “VS 2026 supports VisualStudio.Extensibility” | **Yes** — OOP model works on VS 2022 (~17.9+) and VS 2026 (18.x). Broader API coverage is still evolving (preview-ish for many scenarios). |
| “Can target .NET 10 for OOP extensions” | **Yes on current VS 2026** (Insider rollout early 2026, then stable 18.x). Use `<TargetFramework>net10.0-windows…</TargetFramework>` (or similar) and declare `DotnetTargetVersions` (e.g. `net10.0` / `DotnetTarget.Custom("net10.0")`). |
| “net10 always worked from day one of VS 2026” | **No** — Dec 2025 builds had host gaps ([VSExtensibility#544](https://github.com/microsoft/VSExtensibility/issues/544)); support landed in early/mid-2026 updates. |
| “Default host is already net10-only” | **Not yet.** net8 and net10 **coexist** until .NET 8 LTS ends **2026-11-10**; then the host transitions toward **net10 as primary/default**. |
| “net10-only extension runs everywhere” | **No** — declaring only `net10.0` requires a VS install that provides the net10 host (current VS 2026 yes; older 17.x / early 18.x hosts may still be net8-only). |
| “In-proc VisualStudio.Extensibility is net10” | **No** — hybrid in-process mode still runs on **.NET Framework** inside `devenv` and can call classic VSSDK APIs. |

**Bottom line for Cornerstone:** basic **net10 OOP is available** on current VS 2026. That removes the “wait for modern runtime” excuse for *new* OOP experiments (tool-window chrome, commands, settings). It does **not** by itself make a feature-parity port possible — designer/editor APIs remain the limit.

### Where net10 fits in *this* repo today

| Location | TFM today | Role |
|----------|-----------|------|
| `Cornerstone.VisualStudio` VSIX | `net472` | Shipping IDE integration (classic VSSDK) |
| `Cornerstone.VisualStudio.Core` | `netstandard2.0` | Portable logic (completion engine, parsing, cleanup, metadata) |
| Unit tests / Avalonia sample apps | `net10.0` | Validation and demos |
| **Previewer / designer host process** | Can be **net10** | Interactive preview + net10-only libraries without rewriting the VSIX |
| **Optional future OOP project** | `net10.0-windows…` on VS 2026 | Thin shell (commands, settings, Remote UI chrome) if product accepts subset UX |

For **shipping** full designer + IntelliSense, stay on classic VSSDK until the product accepts a subset (or Microsoft grows the API surface). For **new net10-only libraries**, prefer the **previewer side process** and/or a future OOP sidecar — not the in-proc hybrid path.

---

## What is already portable

These pieces are host-agnostic and should stay that way for any future dual-host or rewrite:

- **`Cornerstone.VisualStudio.Core`** — XAML/XML parsing, completion engine, text manipulation, assembly metadata (dnlib)
- **Unit tests** against Core
- **Previewer process concept** — external host + Avalonia remote protocol (protocol logic is not tied to VSSDK; only the IDE hosting surface is)

Avoid putting new business logic only in VSIX-only types when it can live in Core (or in a net10 side process).

---

## When to revisit

Reassess a move (or a hybrid) when **enough** of the following are true:

1. **Willing to accept non-document preview UX** (tool window and/or external preview window), *or* VisualStudio.Extensibility gains custom multi-view document editors comparable to `IVsEditorFactory`
2. **Product scope that fits OOP APIs** (tool window + commands + settings + optional LSP) — runtime is no longer the gate on current VS 2026
3. **Rich editor language features** without forcing a full LSP rewrite of existing completion — *or* a deliberate decision to adopt LSP
4. **Project / build output hooks** sufficient for assembly resolution and designer target discovery
5. **Install-base policy** if shipping **net10-only** OOP (require VS 2026 hosts with net10; drop older net8-only hosts)

### Optional spikes (measure cost; not a migration plan)

| Spike | Goal | Platform |
|-------|------|----------|
| **A. Classic tool-window preview** | Validate “preview selected tab in a tool window” UX | Stay VSSDK `net472` |
| **B. External net10 preview host** | Run interactive preview UI + new libraries on net10 | Keep thin VSIX; evolve process launched by `PreviewerProcess` |
| **C. OOP shell sidecar** | Commands / settings / thin Remote UI chrome on **net10** | VS 2026 VisualStudio.Extensibility; not required for B; still not full parity |

Track: [microsoft/VSExtensibility](https://github.com/microsoft/VSExtensibility).

Until feature parity (or an accepted subset) is realistic, **stay on classic VSSDK** for shipping. Spikes above are product experiments, not a silent retarget.

---

## References

- [Choose the right Visual Studio extensibility model](https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/extensibility-models)
- [VisualStudio.Extensibility overview](https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/visualstudio-extensibility)
- [Tool windows (VisualStudio.Extensibility)](https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/tool-window/tool-window)
- [Remote UI](https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/inside-the-sdk/remote-ui)
- [Other Remote UI concepts (images, context menus)](https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/inside-the-sdk/other-remote-ui)
- [.NET compatibility / runtime management for VisualStudio.Extensibility](https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/dotnet-management-overview)
- [Managing .NET runtime versions (blog)](https://devblogs.microsoft.com/visualstudio/visualstudio-extensibility-managing-net-runtime-versions/)
- [VSExtensibility#544 — net10 / C# 14 with VisualStudio.Extensibility](https://github.com/microsoft/VSExtensibility/issues/544)
- [Port, migrate, and upgrade Visual Studio projects (incl. VSIX)](https://learn.microsoft.com/en-us/visualstudio/releases/2026/port-migrate-and-upgrade-visual-studio-projects)
- Install targets: `Cornerstone.VisualStudio/source.extension.vsixmanifest`
- VSIX TFM: `Cornerstone.VisualStudio/Cornerstone.VisualStudio.csproj` (`net472`)
- Designer surface today: `Views/EditorPane.cs`, `Views/AvaloniaDesigner.xaml.cs`, `Views/AvaloniaPreviewer.xaml.cs`, `Services/PreviewerProcess.cs`
