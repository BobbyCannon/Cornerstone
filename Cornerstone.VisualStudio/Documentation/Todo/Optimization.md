# Cornerstone Visual Studio Extension — Performance Optimization Plan

Living document for CPU / latency work on the Cornerstone XAML designer and related IDE features.  
Return here when resuming optimization work.

**Context:** Random high CPU while the designer is open; investigation centered on the previewer host process and related IDE-side work.  
**Ship notes:** User-facing changes for the next VSIX go in [../NextRelease.md](../NextRelease.md).

---

## Problem summary

| Symptom | Likely process | Notes |
|---------|----------------|-------|
| CPU while designer idle / open | Host `dotnet` (Designer HostApp) and/or `devenv` | Host render timer runs only while the scene is dirty (up to 120 Hz). Visual Studio presents a frame on the WPF render pass and unhooks while the picture is still |
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
- [x] `CornerstoneDesigner.OnBuildCompletedAsync` / `RecycleHostAsync`: wait-stop → `LoadTargetsAsync` → start + push buffer XAML.
- [x] `EditorPane.HandleBuildDone` calls recycle instead of only flipping `IsPaused`.

### Designer / preview UI

- [x] Skip layout (size/margin) when size/scaling unchanged (`CornerstonePreviewer`).
- [x] Filter tiny mouse moves before sending pointer input to host.
- [x] Fit-zoom feedback break (superseded: Fit modes removed entirely; percentage zoom only, default 100%).
- [x] Lighter `FrameReceived` path (no redundant main-thread hop for trivial show-preview).
- [x] Debounce + skip unchanged XAML to host (`Throttle` classic debounce, `_lastSentXaml`, adaptive idle).
- [x] Suspend host when document tab not visible (`EditorPane` / `IVsWindowFrameNotify3`); Source-only idle suspend (15 s).
- [x] Remote frame pump presents on `CompositionTarget.Rendering` (`RemoteSession`): latest frame wins, once per WPF render, so the cap is the monitor refresh. The 16 ms `DispatcherPriority.Background` timer was removed; it restarted after every blit and skipped frames. Same-size frames update pixels only and do not run layout. The hook is removed while no frame is waiting. `Stop` (tab hidden, Source-only suspend) drops it so background documents do not keep waking `devenv`. Invalid markup still ACKs and does not present.
- [x] Remove Fit All / Fit to Width — fixed % zoom only; drop viewport↔scale coupling and fit SizeChanged path.

---

## Recommended next work (priority order)

### P1 — High impact

#### 1. Debounce + skip unchanged XAML to host

**Files:** `Services/Throttle.cs`, `Views/CornerstoneDesigner.xaml.cs`  
**Why:** Every settled edit still ships full XAML and forces host reload/render.

- [x] Always restart debounce timer on each edit (classic debounce), even when values compare equal if needed.
- [x] Track last successfully sent XAML; skip `UpdateXamlAsync` when unchanged (`_lastSentXaml`; cleared on process stop/exit).
- [x] Longer idle delay for large documents (300 ms default, 500 ms ≥40k chars, 750 ms ≥100k chars).
- [x] Prefer buffer text on start/edit path; avoid redundant file reads when editor buffer is available.
- [x] `UpdateXamlAsync` returns `bool` so transport failures do not poison the skip cache.

#### 2. Stop or suspend host when not needed

**Files:** `Views/CornerstoneDesigner.xaml.cs`, `Views/EditorPane.cs`, `Services/PreviewerProcess.cs`  
**Why:** One 60 Hz design host per open designer; Source-only mode previously kept the process alive forever.

- [x] Stop host when document tab is not visible (2 s delay via `IVsWindowFrameNotify3.OnShow` + `SetDocumentVisible`).
- [x] Stop host when View == Source after 15 s idle; error tagger keeps last `ExceptionDetails`; edit restarts host.
- [x] Intentional suspend does not show “previewer process exited” crash UI (`_hostSuspendedIntentionally`).
- [ ] Longer-term: share one host per target assembly across documents (hard; large multi-tab win).

#### 3. Smarter completion metadata cache

**Files:** `Views/CornerstoneDesigner.xaml.cs` (`CreateCompletionMetadataAsync`, `_metadataCache`), `DnlibMetadataProvider`, `MetadataConverter`  
**Why:** Full assembly walk + convert on open/build is a major spike.

- [ ] Cache key = executable path + reference list hash + assembly write times (not path alone).
- [ ] On build: invalidate only projects/targets that rebuilt, not global `_metadataCache.Clear()` on every `OnBuildBegin`.
- [ ] Lazy-load metadata on first completion request instead of always at designer start.
- [ ] Skip analyzers, design-time-only, satellite, and pure-resource assemblies where safe.
- [ ] Investigate Roslyn / VS reference graph vs re-reading every DLL via dnlib.

#### 4. Remove WPF Gaussian blur “shadow”

**File:** `Views/CornerstonePreviewer.xaml`  
**Why:** `BlurEffect` on a full-size border is expensive when layout/size changes.

- [x] Replaced with simple border shadow (no `BlurEffect` / `DropShadowEffect`).

#### 5. Stop full-document `GetText()` on hot paths

**Files:** `XamlCompletionSource.cs`, `XamlTextManipulatorRegistrar.cs`, `XamlCompletionCommandHandler.cs`, suggested actions  
**Why:** Large AXAML → allocation + GC on typing and completion.

- [x] Completion: do not start a session for letters/spaces in element content (avoids full `GetText()` per keystroke); parse-to-caret instead of full snapshot on session key handlers.
- [x] Manipulator: skip unless the edit looks like markup or the caret is inside an open tag (lookback, not full document).
- [x] Designer `ChangedOnBackground`: debounce without copying the snapshot; read buffer once when idle.
- [x] Error tagger: marshal `ErrorChanged` to the UI thread; `TagsChanged` only the error line(s), not the whole snapshot.
- [x] Frames: ACK same-size frames without UI hop / `WritePixels` when inside the ~16 ms interval.
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

**Files:** `PreviewerProcess.cs`, `CornerstonePreviewer.xaml.cs`  
**Why:** Extra work when user is not looking at the design surface.

- [x] When tab inactive or VS minimized: stop host (via P1#2 visibility suspend).
- [x] Visual Studio frame apply is the WPF render pass. `RemoteSession` hooks only while a frame is queued and drops that hook on `Stop`.
- [ ] Throttle pointer moves to ~30 Hz; skip input when not over preview or when markup-paused.
- [ ] Host: do not send a frame whose pixels match the last sent frame (animations that tick without changing pixels).

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

- [x] Stronger “pause preview while typing” / soft invalid markup — incomplete preflight skip, 500 ms debounce, deferred error overlay, long-idle force send (`XamlEditCompleteness`, `CornerstoneDesigner`).
- [x] Editor hang on document edit — error tagger UI marshal + narrow tags; no full-snapshot work on content keystrokes; throttle `WritePixels` before the UI hop.
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
VS (devenv)                              Host (dotnet Cornerstone.Designer.HostApp)
─────────────────────────────────────    ──────────────────────────────────────
StartAsync → TCP BSON listen             Connect, Design.IsDesignMode
ClientSupportedPixelFormats              UiThreadRenderTimer(120 Hz)
ClientRenderInfoMessage (DPI)            Paint → private pixel copy → FrameMessage
UpdateXaml → load design window          Send the latest frame when the socket write is free
On frame: present on WPF render          ACK caps the pipe at two frames
FrameReceived → WPF Image / layout       BSON frame is one pre-sized write; pixel buffers are reused
```

- Host always has a render loop while the process lives; dirty trees (animations, continuous invalidate) drive frames.
- Invalid markup: host returns `UpdateXamlResult` with exception; VS freezes last good frame (implemented).
- Unhandled design-time app code can still kill the host; UI should pause and restart on edit (implemented).

Key types:

| Area | Primary files |
|------|----------------|
| Host process | `Services/PreviewerProcess.cs` |
| Designer shell | `Views/CornerstoneDesigner.xaml(.cs)` |
| Preview surface | `Views/CornerstonePreviewer.xaml(.cs)` |
| Editor lifecycle | `Views/EditorPane.cs`, `Services/EditorFactory.cs` |
| Debounce | `Services/Throttle.cs` |
| Solution graph | `Services/SolutionService.cs` |
| Completion | `IntelliSense/*`, `Core/Completion/*` |
| Metadata | `Core/DnlibMetadataProvider/*`, `Core/AssemblyMetadata/*` |

---

## Related issues / external context

- Cornerstone designer host continuous frames (historical): [Cornerstone#10203](issues/10203)
- Previewer high CPU reports: [Cornerstone#12438](issues/12438)
- Extension code was largely aligned with archived CornerstoneVS `PreviewerProcess` patterns.

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