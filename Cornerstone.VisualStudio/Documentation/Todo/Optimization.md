# Cornerstone Visual Studio Extension — Performance Optimization Plan

Living document for CPU / latency work on the Avalonia XAML designer and related IDE features.  
Return here when resuming optimization work.

**Last updated:** 2026-07-31  
**Context:** Random high CPU while the designer is open; investigation centered on the previewer host process and related IDE-side work.  
**Ship notes:** User-facing changes for the next VSIX go in [../NextRelease.md](../NextRelease.md).

---

## Problem summary

| Symptom | Likely process | Notes |
|---------|----------------|-------|
| CPU while designer idle / open | Host `dotnet` (Designer HostApp) and/or `devenv` | Host uses a ~60 Hz design-mode render timer; VS applies frames + WPF layout |
| Spikes on open / after build | `devenv` | Metadata (dnlib) + solution graph walk |
| Typing hitches | `devenv` | Full-document copies, completion, manipulators |
| Host dies on bad XAML / app code | Host exits | Should pause UI, not thrash |

**How to measure**

1. Task Manager / Process Explorer: is **`devenv`** or **`dotnet` HostApp** hot?
2. Count HostApp processes with multiple AXAML tabs open.
3. PerfView / VS Diagnostic Hub: scenarios — open designer, edit AXAML, idle with designer open.
4. Cornerstone Diagnostics output pane: keep level at **Information** for normal use; use Verbose only briefly.

---

## Already done (baseline)

These landed during the CPU / stability pass. Do not re-do unless regressing.

### Previewer host / frames (`PreviewerProcess.cs`)

- [x] Always send `ClientRenderInfoMessage` after connect (DPI/scaling no longer skipped when set pre-connect).
- [x] Default `Scaling = 1`; round scaling; epsilon to avoid noise re-renders.
- [x] Serialize / coalesce frame handling; drop intermediate pending frames with ACK so host is not stalled.
- [x] Throttle UI `FrameReceived` notifications (~60 FPS; was ~15 before single-live-host); still ACK host.
- [x] Never log full `FrameMessage` pixel buffers (sequence / size / format only).
- [x] NetCore host gets `WorkingDirectory = executableDir`.
- [x] Process hygiene: detach handlers, dispose, re-entrant `Stop`, clear `_process`.

### Freeze on invalid markup

- [x] While `Error != null`, ACK frames but do not `WritePixels` / notify UI (freeze last good frame).
- [x] Ignore degenerate 1×1 frames when a good bitmap already exists.
- [x] Soft-fail `UpdateXamlAsync` transport errors into markup pause instead of unhandled faults.
- [x] Designer shows real error text (line/col) + “paused on last valid frame”.
- [x] Host process exit → “Preview Paused”; restart host on next edit when not running.

### Post-build host recycle (design data)

- [x] **Root cause:** BuildBegin pauses + `Kill()`; BuildDone unpaused via `IsPaused` and `StartStopProcessAsync` only if `!IsRunning`. Kill is async; unpause often skipped start; intentional `ProcessExited` did not restart → frozen last frame / old assemblies. C# `CreateDesignData` never reloads via `UpdateXaml` alone.
- [x] `PreviewerProcess.StopAndWaitAsync` waits for exit before restart.
- [x] `AvaloniaDesigner.OnBuildCompletedAsync` / `RecycleHostAsync`: wait-stop → `LoadTargetsAsync` → start + push buffer XAML.
- [x] `EditorPane.HandleBuildDone` calls recycle instead of only flipping `IsPaused`.

### Designer / preview UI

- [x] Skip layout (size/margin) when size/scaling unchanged (`AvaloniaPreviewer`).
- [x] Filter tiny mouse moves before sending pointer input to host.
- [x] Fit-zoom feedback break (superseded: Fit modes removed entirely; percentage zoom only, default 100%).
- [x] Lighter `FrameReceived` path (no redundant main-thread hop for trivial show-preview).
- [x] Debounce + skip unchanged XAML to host (`Throttle` classic debounce, `_lastSentXaml`, adaptive idle).
- [x] Suspend host when document tab not visible (`EditorPane` / `IVsWindowFrameNotify3`); Source-only idle suspend (15 s).
- [x] Remove Fit All / Fit to Width — fixed % zoom only; drop viewport↔scale coupling and fit SizeChanged path.

---

## Recommended next work (priority order)

### P1 — High impact

#### 1. Debounce + skip unchanged XAML to host

**Files:** `Services/Throttle.cs`, `Views/AvaloniaDesigner.xaml.cs`  
**Why:** Every settled edit still ships full XAML and forces host reload/render.

- [x] Always restart debounce timer on each edit (classic debounce), even when values compare equal if needed.
- [x] Track last successfully sent XAML; skip `UpdateXamlAsync` when unchanged (`_lastSentXaml`; cleared on process stop/exit).
- [x] Longer idle delay for large documents (300 ms default, 500 ms ≥40k chars, 750 ms ≥100k chars).
- [x] Prefer buffer text on start/edit path; avoid redundant file reads when editor buffer is available.
- [x] `UpdateXamlAsync` returns `bool` so transport failures do not poison the skip cache.

#### 2. Stop or suspend host when not needed

**Files:** `Views/AvaloniaDesigner.xaml.cs`, `Views/EditorPane.cs`, `Services/PreviewerProcess.cs`  
**Why:** One 60 Hz design host per open designer; Source-only mode previously kept the process alive forever.

- [x] Stop host when document tab is not visible (2 s delay via `IVsWindowFrameNotify3.OnShow` + `SetDocumentVisible`).
- [x] Stop host when View == Source after 15 s idle; error tagger keeps last `ExceptionDetails`; edit restarts host.
- [x] Intentional suspend does not show “previewer process exited” crash UI (`_hostSuspendedIntentionally`).
- [ ] Longer-term: share one host per target assembly across documents (hard; large multi-tab win).

#### 3. Smarter completion metadata cache

**Files:** `Views/AvaloniaDesigner.xaml.cs` (`CreateCompletionMetadataAsync`, `_metadataCache`), `DnlibMetadataProvider`, `MetadataConverter`  
**Why:** Full assembly walk + convert on open/build is a major spike.

- [ ] Cache key = executable path + reference list hash + assembly write times (not path alone).
- [ ] On build: invalidate only projects/targets that rebuilt, not global `_metadataCache.Clear()` on every `OnBuildBegin`.
- [ ] Lazy-load metadata on first completion request instead of always at designer start.
- [ ] Skip analyzers, design-time-only, satellite, and pure-resource assemblies where safe.
- [ ] Investigate Roslyn / VS reference graph vs re-reading every DLL via dnlib.

#### 4. Remove WPF Gaussian blur “shadow”

**File:** `Views/AvaloniaPreviewer.xaml`  
**Why:** `BlurEffect` on a full-size border is expensive when layout/size changes.

- [x] Replaced with simple border shadow (no `BlurEffect` / `DropShadowEffect`).

#### 5. Stop full-document `GetText()` on hot paths

**Files:** `XamlCompletionSource.cs`, `XamlTextManipulatorRegistrar.cs`, `XamlCompletionCommandHandler.cs`, suggested actions  
**Why:** Large AXAML → allocation + GC on typing and completion.

- [ ] Completion: pass only text-to-caret (or reuse engine’s substring) without double materialization.
- [ ] Manipulator: only run for changes that can affect structure; scope to changed line ± window.
- [ ] Command handler: avoid full-snapshot parse on every commit key; cache parse for caret line/position.
- [ ] Suggested actions: cache xmlns aliases until buffer version changes.

---

### P2 — Medium impact

#### 6. Cache solution / project graph

**File:** `Services/SolutionService.cs`  
**Why:** `GetProjectsAsync` is main-thread heavy (DTE, CPS reflection, MSBuild properties).

- [ ] Cache `ProjectInfo` list; refresh on project load/unload, reference change, build done, active config change.
- [ ] Avoid full solution walk on every designer start / target reload when cache is warm.

#### 7. Completion session lifecycle (perf + correctness)

**Files:** `XamlCompletionCommandHandler.cs`, `XamlCompletionSource.cs`  
**Why:** `Filter()` / `Start()` churn; related to `ShimCompletionController.RecalculateSession` NRE.

- [ ] Never call `Start()` again on an already-started session.
- [ ] Avoid double-subscribe to `Dismissed`.
- [ ] Null-guard `SelectedCompletionSet` / selected completion before commit.
- [ ] Validate `ApplicableTo` span (`start` in range, non-negative length).
- [ ] Materialize completion list before building `CompletionSet`.
- [ ] Reduce unnecessary `session.Filter()` calls.

#### 8. Preview frame / input policy when inactive

**Files:** `PreviewerProcess.cs`, `AvaloniaPreviewer.xaml.cs`  
**Why:** Extra work when user is not looking at the design surface.

- [x] When tab inactive or VS minimized: stop host (via P1#2 visibility suspend).
- [ ] Throttle pointer moves to ~30 Hz; skip input when not over preview or when markup-paused.

#### 9. Logging policy

**Files:** `CornerstonePackage.cs`, `PreviewerProcess.cs`, `OutputPaneEventSink.cs`  
**Why:** High-frequency Debug/Verbose to output pane and Trace is costly.

- [ ] Keep default at Information.
- [ ] Ensure Verbose is not written to the VS output pane in shipping defaults.
- [ ] Avoid Trace for frame/message spam paths.

#### 10. Text manipulator scoping

**File:** `XamlTextManipulatorRegistrar.cs`  
**Why:** Full document + manipulators on every buffer change.

- [ ] Gate on relevant change kinds / characters.
- [ ] Use span-local text where possible.

---

### P3 — Lower impact / polish

- [x] Stronger “pause preview while typing” / soft invalid markup — incomplete preflight skip, 500 ms debounce, deferred error overlay, long-idle force send (`XamlEditCompleteness`, `AvaloniaDesigner`).
- [ ] Document that design-time animations / clocks keep the host hot; optional “disable animations in designer” if AppBuilder can be influenced.
- [ ] Parallel dnlib assembly reads only if measured safe (I/O-bound).
- [ ] Error tagger: keep full-snapshot `TagsChanged` only while single diagnostic; narrow span if multi-diag later.
- [ ] `WriteableBitmap` size-stable path: keep verifying no per-frame recreation.

---

## Suggested implementation order (next PR(s))

| Order | Item | Effort | Expected win |
|------:|------|--------|--------------|
| 1 | Debounce + skip identical XAML | Small | Less host render while typing — **done 2026-07-31** |
| 2 | Stop host when tab not visible | Medium | Multi-tab / idle CPU — **done 2026-07-31** |
| 3 | Remove BlurEffect | Trivial | WPF layout cost — **done** |
| 4 | Metadata cache + selective invalidate | Medium | Open/build spikes |
| 5 | Reduce full-document GetText / parse | Medium | Typing latency |
| 6 | Cache GetProjectsAsync | Medium | Designer start |
| 7 | Completion session hardening | Medium | Perf + Recalculate NRE |
| 8 | Shared host (optional) | Large | Many open AXAML files |

---

## Architecture notes (previewer)

```
VS (devenv)                              Host (dotnet Avalonia.Designer.HostApp)
─────────────────────────────────────    ──────────────────────────────────────
StartAsync → TCP BSON listen             Connect, Design.IsDesignMode
ClientSupportedPixelFormats              UiThreadRenderTimer(~60 Hz)
ClientRenderInfoMessage (DPI)            Paint → FrameMessage
UpdateXaml → load design window          Wait for FrameReceivedMessage ACK
On frame: WritePixels (if not frozen)    Flow-controlled by ACK
FrameReceived → WPF Image / layout
```

- Host always has a render loop while the process lives; dirty trees (animations, continuous invalidate) drive frames.
- Invalid markup: host returns `UpdateXamlResult` with exception; VS freezes last good frame (implemented).
- Unhandled design-time app code can still kill the host; UI should pause and restart on edit (implemented).

Key types:

| Area | Primary files |
|------|----------------|
| Host process | `Services/PreviewerProcess.cs` |
| Designer shell | `Views/AvaloniaDesigner.xaml(.cs)` |
| Preview surface | `Views/AvaloniaPreviewer.xaml(.cs)` |
| Editor lifecycle | `Views/EditorPane.cs`, `Services/EditorFactory.cs` |
| Debounce | `Services/Throttle.cs` |
| Solution graph | `Services/SolutionService.cs` |
| Completion | `IntelliSense/*`, `Core/Completion/*` |
| Metadata | `Core/DnlibMetadataProvider/*`, `Core/AssemblyMetadata/*` |

---

## Related issues / external context

- Avalonia designer host continuous frames (historical): [Avalonia#10203](https://github.com/AvaloniaUI/Avalonia/issues/10203)
- Previewer high CPU reports: [Avalonia#12438](https://github.com/AvaloniaUI/Avalonia/issues/12438)
- Extension code was largely aligned with archived AvaloniaVS `PreviewerProcess` patterns.

---

## Open questions

1. ~~Is keeping the host alive in Source-only mode still required for the error tagger?~~ **Resolved:** tagger retains last `ExceptionDetails`; host idle-suspends in Source-only and restarts on edit.
2. ~~Should Fit zoom remain available by default, or default to 100%?~~ **Resolved:** Fit All / Fit to Width removed; percentage zoom only, default 100%. Legacy Fit settings coerce to 100%.
3. ~~Target multi-document: shared host vs stop-on-background first?~~ **Resolved for now:** stop-on-background first (shared host still optional later).
4. Completion NRE (`ShimCompletionController.RecalculateSession`): reproduce steps still needed for a focused fix PR.

---

## Checklist when closing an optimization PR

- [ ] Scenario: idle designer open — host + devenv settle.
- [ ] Scenario: type in large AXAML — no multi-second freezes.
- [ ] Scenario: invalid markup — freeze last frame, resume when fixed.
- [ ] Scenario: host process exit — pause UI; edit restarts host.
- [ ] Scenario: multiple AXAML tabs — host count and idle CPU acceptable.
- [ ] Logging default remains Information; no frame pixel dumps.
- [ ] Update this file: mark completed items, add regressions/learnings.