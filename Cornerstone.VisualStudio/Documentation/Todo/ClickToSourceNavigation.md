# Design: Click preview element → navigate to AXAML

**Status:** Deferred — research only; no implementation planned yet.  
**Date:** 2026-08-12  
**Area:** Cornerstone.VisualStudio designer / previewer

## Goal

From the Avalonia designer preview surface, click (or Ctrl+click) a control and have Visual Studio move the caret to the corresponding markup in the open `.axaml` document (WPF-style “select element”).

## Bottom line

| Approach | Effort | Fidelity | Needs Avalonia fork/PR? |
|----------|--------|----------|-------------------------|
| A. IDE-only heuristic (no host help) | Not viable | N/A | — |
| B. Host hit-test → type/name/selector → IDE XML match | **Medium** (≈1–2 weeks) | OK for simple trees; weak for templates/lists | No (custom host or small host wrapper) |
| C. Host hit-test → `XamlSourceInfo` line/col → IDE navigate | **Medium–Hard** (≈2–4 weeks) | High (closest to WPF) | Prefer custom designer host; optional upstream PR |
| D. Wait for official Avalonia/VS feature | Unknown | Ideal eventually | Team wants it; previewer still “preview only” ([discussion #13956](https://github.com/AvaloniaUI/Avalonia/discussions/13956)) |

**Verdict:** Doable, but **not a small VS-extension-only tweak**. The IDE half is easy; the hard half is **getting source location out of the designer host process**. Avalonia already has runtime source metadata (`XamlSourceInfo`), but the stock designer host **does not enable it** and the remote protocol has **no hit-test/navigate messages**.

---

## Current architecture (Cornerstone)

```
┌─────────────────────────────────────────────────────────────┐
│ VS process (net472 VSIX)                                    │
│  AvaloniaDesigner + AvaloniaPreviewer (WPF Image)           │
│       │ pointer events                                      │
│       ▼                                                     │
│  PreviewerProcess  ──BSON TCP──►  Avalonia.Designer.HostApp │
│  UpdateXaml / frames / input     (loads user app + XAML)    │
└─────────────────────────────────────────────────────────────┘
```

Key files:

- `Cornerstone.VisualStudio/Views/AvaloniaPreviewer.xaml.cs` — mouse down/move/up → `Pointer*EventMessage` only (no selection mode).
- `Cornerstone.VisualStudio/Services/PreviewerProcess.cs` — process + `Avalonia.Remote.Protocol` transport.
- `Cornerstone.VisualStudio/Views/AvaloniaDesigner.xaml.cs` — owns host lifecycle, XAML push, error overlay.
- Host: **`Avalonia.Designer.HostApp`** → `RemoteDesignerEntryPoint` → `DesignWindowLoader.LoadDesignerWindow`.

Protocol surface (Avalonia 12.x) is limited: `UpdateXamlMessage` / `UpdateXamlResultMessage`, viewport/frames, input events, `StartDesignerSessionMessage`. **No “element at point” / “source location” messages.**

Pointer clicks today **drive the live preview** (buttons work, etc.); they do not select markup.

Related docs: [ExtensibilityPlatform.md](../ExtensibilityPlatform.md), [Todo/Optimization.md](Optimization.md).

---

## Avalonia pieces that matter

### Runtime source metadata

`Avalonia.Markup.Xaml.Diagnostics.XamlSourceInfo`:

- `SourceUri`, `LineNumber`, `LinePosition` (1-based)
- `GetXamlSourceInfo(object)` / `SetXamlSourceInfo(...)`

The runtime XAML compiler can attach this when:

```csharp
new RuntimeXamlLoaderConfiguration { CreateSourceInfo = true, DesignMode = true, ... }
```

**Default is `false`.** Stock `DesignWindowLoader` sets `DesignMode = true` but **does not** set `CreateSourceInfo = true`, so designer-loaded trees currently have **no** line mapping unless the host is changed.

Compile-time MSBuild `AvaloniaXamlCreateSourceInfo` applies to **compiled** XAML, not the designer’s runtime reload path. The designer reloads the **text** sent via `UpdateXamlMessage`.

### Hit testing

In-process Avalonia can resolve a visual at a point (`GetVisualsAt` / `InputHitTest`). That only works **inside the host**, not against the WPF `WriteableBitmap` in VS.

### Protocol extensibility

`DefaultMessageTypeResolver` accepts extra assemblies; types marked with `AvaloniaRemoteMessageGuidAttribute` can be custom BSON messages. Cornerstone can define `HitTestRequest` / `HitTestResult` without waiting for Avalonia—if both ends load those types.

---

## How `Avalonia.Diagnostics` fits (Cornerstone fork of DevTools)

Path: `Cornerstone/Avalonia.Diagnostics` (net10.0). This is the **in-app DevTools** UI (F12-style), **not** part of the VS extension.

### Already solved there (steal / share)

| Capability | Where | Use for click→AXAML |
|------------|--------|---------------------|
| **Pick control under pointer** | `MainWindow.GetHoveredControl` | Host hit-test core |
| **Filter adorners / invisible / non-hittest** | `GetVisualsAt(point, filter)` | Avoid selecting highlight chrome |
| **Popup / flyout / tooltip roots** | `GetPopupRoots` | Correct target when preview has popups |
| **Select in tree (walk parents)** | `TreePageViewModel.SelectControl` | If leaf has no source, climb |
| **DevTools selector string** | `GetVisualSelector` → `{asm}ns\|Type#name.classes` | Same format already parsed in VS Core |
| **Visual highlight** | `ControlHighlightAdorner` | Optional flash of selected control in host |

Pick algorithm (reference):

```csharp
// MainWindow.axaml.cs — GetHoveredControl
return (Control) topLevel.GetVisualsAt(point, x =>
{
    if (x is AdornerLayer || !x.IsVisible) return false;
    return !(x is IInputElement ie) || ie.IsHitTestVisible;
}).FirstOrDefault();
```

Selector format already has IDE-side support:

- Host builds: `{Assembly}Namespace|TypeName#name.classes` (`TreePageViewModel.GetVisualSelector`)
- VS Core parses: `Cornerstone.VisualStudio.Core.Parsing.DevToolsSelectorParser` / `DevToolsSelectorInfo`

### What Diagnostics does **not** provide

- **No `XamlSourceInfo` / file+line+col** — runtime inspection only; never navigates to markup.
- **No VS / Remote.Protocol bridge** — lives in the app process (or DevTools window), not HostApp ↔ VSIX.
- **Not loaded in design mode by default** — `DevTools.Attach(Application, …)` **skips when `Design.IsDesignMode`**. Designer host always uses design mode. Reuse **algorithms**, do not open full DevTools in the designer host.
- **Heavy UI** — do not project-reference full Diagnostics from the net472 VSIX. Extract a thin pick helper if sharing code with DevTools is desired.

### Impact on approaches

- **Approach B** is cheaper: selector string + `DevToolsSelectorParser` + XML match is a natural MVP.
- **Approach C** still needs `CreateSourceInfo` + host protocol; Diagnostics only supplies the **pick** step before `XamlSourceInfo.GetXamlSourceInfo`.

---

## Hard parts vs easy parts

### Easy (IDE / VSIX)

1. Gesture — e.g. Ctrl+Click (or Alt+Click) for “select source”; plain click keeps interactive preview.
2. Caret navigation — already host `IVsCodeWindow` / `IVsTextView` via `TextEditorHost` / `EditorPane`.
3. Same-document case — preview is usually the open buffer.
4. UX polish — flash highlight, status bar text, quiet failure when no source.

### Medium / hard (host)

1. Custom protocol messages for hit-test request/response.
2. Custom designer host that enables `CreateSourceInfo = true` and handles hit-test (copy/adapt `DesignWindowLoader` / entry point).
3. Ship / discover host — today targets resolve `Avalonia.Designer.HostApp` from NuGet; either bundle **Cornerstone.Designer.HostApp** in the VSIX or contribute upstream.
4. Coordinate systems — zoom, DPI, margins in `AvaloniaPreviewer` must match host logical pixels.
5. Edge cases: templates/ItemsControl, controls without source info, nested UserControls (`SourceUri` → other file), invalid markup / paused preview.

### Not viable alone

Matching only from a bitmap pixel in the VS process with no host query: **no element identity**.

---

## Recommended architecture (Approach C)

```
Ctrl+Click on preview (VS)
  → PreviewerProcess.Send(HitTestRequestMessage { X, Y })
  → Custom HostApp (pick logic ~ Diagnostics GetHoveredControl):
       GetVisualsAt + filters; popup roots if needed
       walk parents for XamlSourceInfo.GetXamlSourceInfo(node)
       optional: ControlHighlightAdorner-style flash
       → HitTestResultMessage { File, Line, Column, TypeName, Selector? }
  → AvaloniaDesigner:
       if File is current buffer → SetCaret
       else open document + navigate
```

- **Protocol package:** small shared `Cornerstone.VisualStudio.Previewer.Protocol` (netstandard2.0) referenced by VSIX and HostApp; GUID-attributed message types.
- **HostApp:** thin clone of Avalonia’s remote designer entry + loader with `CreateSourceInfo = true` + hit-test handler. Launch via existing `PreviewerProcess.StartAsync(..., hostAppPath, ...)`.
- **Fallback:** stock host; feature disabled with a clear log line.

---

## Approach B (lighter MVP, worse quality)

Skip `XamlSourceInfo`; host returns a **DevTools-style selector** (same as Diagnostics `GetVisualSelector`) and/or name/type path.

IDE uses `DevToolsSelectorParser` + XML parsing to find a best-match element.

| Pros | Cons |
|------|------|
| No dependency on CreateSourceInfo | Ambiguous when many same-type siblings |
| Reuses Diagnostics selector dialect + VS Core parser | Breaks without `x:Name` / unique structure |
| Faster to demo | Templates / DataTemplates messy |

Useful as a **spike** to prove gesture + protocol + caret, then upgrade to C.

---

## Effort sketch (Approach C)

| Slice | Work |
|-------|------|
| Spike: custom messages + stub host reply | 1–2 days |
| Host: CreateSourceInfo + hit-test + walk | 2–4 days |
| VSIX: Ctrl+Click, coord transform, navigate | 1–2 days |
| Multi-file SourceUri + open document | 1–2 days |
| Hardening (zoom/DPI, templates, paused, tests) | 3–5 days |
| Packaging HostApp in VSIX / target resolution | 1–2 days |

**Rough total:** ~2–4 weeks for a reliable feature; a rough spike can validate CreateSourceInfo under designer load in **1–2 days**.

---

## Risks

1. **Avalonia version coupling** — HostApp must match user’s Avalonia major (targets already resolve host per output).
2. **Upstream drift** — reimplementing `RemoteDesignerEntryPoint` means tracking Avalonia designer changes.
3. **PR alternative** — contribute `CreateSourceInfo=true` + optional hit-test messages upstream; longer calendar time, less packaging burden.
4. **Official stance** — Avalonia wants click-to-source but treats previewer as non-designer today; do not block product work on them if the feature is prioritized later.

---

## Suggested future spike (when unblocked)

1. One-off or patched HostApp: load sample XAML with `CreateSourceInfo = true`, hit-test a point (copy `GetHoveredControl` from Diagnostics), print `XamlSourceInfo` + DevTools selector.
2. Confirm designer-loaded controls get line/col (not only compile-time apps).
3. If yes → protocol + VSIX gesture; if no → runtime loader / baseUri (`XamlFileProjectPath` already sent from IDE). Optionally fall back to Approach B.

---

## Open decisions (when revisiting)

1. **Gesture:** Ctrl+Click vs dedicated “select element” mode (toolbar toggle)?
2. **Scope:** same-file only first, or also navigate into other `.axaml` via `SourceUri`?
3. **Hosting:** ship Cornerstone HostApp in VSIX vs Avalonia PR first?
4. **MVP:** Approach B (selector heuristic) for a demo, or straight to C (`XamlSourceInfo`)?
5. **Code reuse:** copy pick helpers vs extract a small shared library from `Avalonia.Diagnostics`?

**Default recommendation (when implementing):** spike CreateSourceInfo + Diagnostics-style pick → Approach C with bundled HostApp; Ctrl+Click; same-file first; copy pick helpers (don’t drag full Diagnostics into the host).
