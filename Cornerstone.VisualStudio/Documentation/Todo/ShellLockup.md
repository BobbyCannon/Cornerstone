# Shell lockup — leads

Symptom: Visual Studio stops taking input, and a key or a window resize makes it respond again. A build can sit until the Visual Studio window is activated and Ctrl is pressed. It cannot be forced. Wait for it, then use the checklist at the bottom.

Resume from this file. The walk of the extension is [../HowItWorks.md](../HowItWorks.md). This page is indexed from [../Readme.md](../Readme.md).

Steady-state Cornerstone frame copies stay at Background. The scheduled flag stays set until `CopyOnUi` returns. Buffer edits post one Background callback per burst. The status-bar gear steps on a Background timer. Do not move steady-state copies back to Input, and do not subscribe to `CompositionTarget.Rendering`.

The stall trace was removed. Do not add it back to chase this symptom. The calls that held the UI thread were Visual Studio's: `ScrollIntoViewWhenPossiblyUnloaded`, `CaretAdornmentLayer.QueueScroll`, `WpfTextView.QueueSpaceReservationStackRefresh`, `JoinableTaskFactory.SingleExecuteProtector`, and `VsUINormalPriorityScheduler.OnTaskQueued`. Each was one call that did not return. None of them is in this repository, and `CopyOnUi` was not the call on the stack. The two `JoinableTaskFactory.Run` sites are now `RunAsync`.

Those Visual Studio calls run because this extension builds the text view. `VsCodeWindowHost` used to call `CreateUIElementPane` inside `Loaded`, which arranges the view before it has a size. `ScrollIntoViewWhenPossiblyUnloaded` then waits on that stack until a window message arrives. Hosting is now posted at Background, and only after the host control has a non-zero size. `CornerstoneDesigner.UpdateLayoutForView` no longer calls `UpdateLayout`. That call arranged the same text view synchronously, which is the `QueueScroll` wait. A resize or a key still wakes a call that is already stuck. It does not undo the hosting that started the call.

Leads 1, 2, 4, and 8 are addressed in `RemoteSession`, `CornerstoneDesigner`, and `VsCodeWindowHost`. Lead 5 is addressed for buffer edits and the gear. Leads 3, 6, and 7 are still open. Lead 3 no longer posts each frame at Input.

WPF dispatcher order, highest first: Send (10), Normal (9), Render (7), Loaded (6), Input (5), Background (4). Loaded through Send are foreground. WPF posts a dispatcher message for each of them even while a key is waiting. Input and Background yield while a key or a posted message is already waiting, and WPF arms a 1 ms timer instead of posting. `WM_SIZE` is still a Win32 message, which is why a resize can paint while those posts are running.

---

## 1. Cornerstone frames were posted above the keyboard

**Addressed.** The first `CopyOnUi` after attach, resume, or a new listen is posted at Input so a restored document paints. Every later copy is posted at Background. `_copyScheduled` stays set until that copy returns, so a frame that arrives during `WritePixels` cannot queue another Input operation into the same drain. If one arrived, the copy posts one Background follow-up. There is no `CompositionTarget.Rendering` subscription and no full-bitmap dirty rect. A restored document keeps the latest frame across suspend, and resume uses one Input copy.

Do not chain Input copies from `CopyOnUi`. A chain of Input copies, with no key down, still posts `DispatcherProcessQueue`. Those posts sit in front of a build. Ctrl makes input pending, the posts stop, and the build runs. That is the stall where selecting the Visual Studio window and pressing Ctrl lets a build continue. Do not move the first paint to Loaded, Normal, or Render, and do not move the steady-state copy back to Input. Do not subscribe to `CompositionTarget.Rendering`.

**Was.** `RemoteSession.EnsurePump` posted `StartPresentOnUi` at Render, and later the first copy moved to Loaded. `StartPresentOnUi` subscribed `CompositionTarget.Rendering`, copied pixels there, and dirtied the whole bitmap while a frame was waiting. Build progress paints entered that hook. The dirty rect scheduled another paint, so the shell queue never drained.

`Cornerstone.Presentation.Remote.Wpf/RemoteSession.cs`

The Avalonia path does not do this. `PreviewerProcess.ApplyFrameOnUiAsync` copies at Background. The status bar and the process panel also post at Background.

**When it would match the symptom.** A preview that keeps producing frames: animation, a blinking caret, a clock, or a compositor that never drops its render-loop item. The designer host arms `UiThreadRenderTimer` at 120 Hz while the compositor has work (`Cornerstone.Designer.HostApp` / `UiThreadRenderTimer`). A static preview should stop. `Framebuffer.CopyRendered` returns null until a new paint marks the buffer `Rendered`.

**When it would not.** One frame after an edit. One Input copy drains. That is a hitch, not a lockup that waits for Ctrl.

**How to confirm.** A build that stalls until Ctrl, or a shell that ignores the keyboard until a resize. Break on the UI thread. `CopyOnUi` may be on the stack. `CompositionTarget.Rendering`, `WakePresent`, and `AddDirtyRect` must not be. A static page that still locks points at lead 5 or lead 6.

---

## 2. Size changes run layout inside the render pass

**Addressed.** `FrameReceived` runs on the Input copy, and only when the bitmap instance changes (first frame, or a new width or height). That is not inside `CompositionTarget.Rendering`.

**Was.** `ProcessPendingFrameOnUi` ran from `OnCompositionRendering` and raised `FrameReceived` on that stack when the bitmap instance changed (first frame, or a new width or height).

Subscribers then run before the render pass returns:

- `RemoteView.ApplyBitmap` sets `Image.Width` and `Image.Height` when the size differs.
- `PreviewerProcess.OnRemoteFrameReceived` forwards to `CornerstonePreviewer.Update`. `Update` calls `ApplyProcessBitmap` immediately when it is already on the UI thread, and that sets the surface size and `PreviewGrid.Margin`.

That is layout during `CompositionTarget.Rendering`. The hosted editor is no longer resized on that stack (lead 8). The layout property changes are still on the render stack.

**When it would match.** Opening a designer (the first real frame replaces the empty bitmap), changing zoom, or a DPI change. The shell sticks until a resize forces a new layout pass.

**How to confirm.** Stuck immediately on first paint of a static page, and a same-size frame later does not stick. Break in `RemoteView.ApplyBitmap`. `CompositionTarget.Rendering` on the stack means the old pump is back.

---

## 3. The frame acknowledgement does not wait for the UI

**Still open, and no longer an Input chain.** The host can still paint every tick, because the acknowledgement is sent before the copy. Each tick is one Background copy when the previous copy is not already scheduled. Background yields while a message is waiting, so this does not reproduce the Ctrl-to-unblock stall. Delaying the acknowledgement until after the copy would slow the host to the UI thread. That is a behavior change. Do not do it to chase lead 1.

**Was an amplifier for lead 1.** `RemoteSession.OnMessage` sends `FrameReceivedMessage` as soon as the bytes are copied, on the socket thread. The host allows two unacknowledged frames (`RemoteServerTopLevelImpl.MaxFramesAwaitingAck`) and sends again when the acknowledgement arrives or the socket write completes.

The cap limits how many frames sit on the wire. It does not limit how fast the next frame is produced once the acknowledgement is back. If the host paints every tick, Visual Studio’s Render queue refills without waiting for the previous copy to finish.

**How to confirm.** Same setup as lead 1. If acknowledgements were delayed until after `ProcessPendingFrameOnUi`, the host would slow to the UI thread. That experiment is a behavior change, not something this note applies.

---

## 4. A hidden document keeps painting for about 2 seconds

**Addressed.** `UpdatePreviewPump` calls `SuspendPreviewSurface` as soon as the tab is hidden or the view is Source. The process still exits on the 2-second (hide) or 15-second (source) timer. Presenting does not stay up for that wait. Showing Design or Split calls `ResumePreviewSurface`.

**Was.** `CornerstoneDesigner` used `BackgroundStopDelay` (2 seconds) after hide, deactivate, or minimize. `SuspendHost` ran when that timer ticked. Until then the host was alive and lead 1 still applied.

Close is different. `EditorPane.OnClose` calls `SuspendPreviewForClose`, which stops the pump immediately and stops the process off the UI thread.

**When it would match.** Switching away from a busy preview, or minimizing, and the shell sticks for a short time or until a resize. After the 2 seconds the pump should drop. A stick that lasts with no designer visible, and no preview process left, is not this lead.

**How to confirm.** Process panel (View → Other Windows → Cornerstone) after the stick. A preview row still Running, for a document that is not the active tab, supports this. No preview row, and the editor host Idle, points away from the frame pump.

---

## 5. `SwitchToMainThreadAsync` is still above Input

**Buffer edits and the gear are addressed.** `ChangedOnBackground` used to post `SwitchToMainThreadAsync` (Normal) once per change. That is now one Background post per burst, and it only arms the debounce timer. The status-bar gear used a forever `DoubleAnimation`, which ticks at Render for the whole editor-host call. It now steps on a Background timer.

**Still watched.** The Visual Studio helper posts at Normal. These call sites use it:

- `OnBuildCompletedAsync` and the error-overlay helpers
- `EditorPane.UpdatePreviewLiveTabCaptionAsync`
- `XamlGoToDefinitionCommand` / `XamlGoToDefinitionNavigator` after the host returns
- `XamlErrorTagger` when applying tags
- `CornerstoneSettingsBridge` pulls

Each call is one post, then the method returns. That drains. It matches a resize-only wake-up only if the posts never stop (a buffer-change loop, or a tagger that invalidates itself).

The frame, gear, and process-panel paths were moved to Background specifically because Normal starved input. These call sites were left on the helper. They are recorded so a fix for lead 1 does not treat them as already safe under a storm of edits.

---

## 6. Go to Definition hand-off blocks the UI thread

**Addressed.** `XamlGoToDefinitionCommand.HandOff` and the off-UI-thread path in `CornerstoneSettingsBridge.PullFromExtensibilityBlocking` posted with `JoinableTaskFactory.RunAsync` and returned. `Run` blocked inside `SingleExecuteProtector`, which is one of the calls the stall trace caught. Do not put `Run` back. The caret and scroll stalls are separate. They are the hosted editor, and this change does not touch them.

Completion and Enter do not wait. `XamlCompletionCommandHandler` returns as soon as the host request is started. A slow editor host delays the list. It does not hold the UI thread for the host timeout.

---

## 7. Verbose logging and the output pane

**Conditional.** `OutputPaneEventSink.Emit` calls `IVsOutputWindowPane.OutputStringThreadSafe` for every event the logger accepts. Default verbosity is Information. Per-frame lines are Verbose. Information logs on a frame only for the first paint and for a size change (`PreviewerProcess.NoteFramePainted`).

If the Cornerstone log option is raised to Verbose, a frame stream becomes an output-pane stream as well. Confirm only when the option is above Information.

One stall cleared without moving the main window. The Output window's "Show output from" dropdown was opened and the down arrow changed the selected pane. The trace had been busy for 182 seconds with no named callback. The shell then stayed idle. Treat the visible output pane as a suspect on the next cold start, including the Build pane while a solution rebuild is writing it. `OutputPaneEventSink` also activates the Cornerstone pane when the package loads.

---

## 8. The hosted editor was resized during layout

**Addressed.** `CodeWindowHwndHost.OnWindowPositionChanged` does not call the base method and does not call `SetWindowPos` on the UI thread. It stores the latest rect and posts one Background callback, `ApplyPendingBounds`. That callback queues `SetWindowPos` on a thread-pool thread with `SWP_ASYNCWINDOWPOS`. The calling thread does not own the editor window, so the flag posts the size onto the UI thread's normal message pump. A rect that was already applied is skipped. `HwndHost` still calls `OnWindowPositionChanged` on every layout pass. The queue flag collapses those calls to one callback.

Posting `SetWindowPos` on the UI thread, including at Background, was not enough. The editor window belongs to that thread, so the call stays synchronous. Each open designer then nests a size handler, and the shell stays stuck until a move unwinds them. Several open documents can take several moves. Do not move `SetWindowPos` back onto the UI thread.

**Was.** The override called `base.OnWindowPositionChanged` first. The base method calls `SetWindowPos` with `SWP_ASYNCWINDOWPOS`. That flag posts only when the window belongs to another thread. `CreatePaneWindow` runs on the Visual Studio UI thread, so the call is synchronous. `WM_WINDOWPOSCHANGED` and `WM_SIZE` then run inside WPF layout, while the dispatcher is not processing. The editor re-enters the shell and the UI stays stuck until another window message arrives. Moving the window is that message. The override also called `SetWindowPos` again at (0, 0) when the width or height changed, which fought the rect layout had just assigned.

A key does not have to clear this. The thread is inside the editor's size handler, not draining a dispatcher queue that yields to input. The frame-copy pass cannot fix it.

**When it would match.** A designer is open, so the code window is hosted. The shell ignores input until the Visual Studio window is moved or resized. The Cornerstone output pane says "Code window hosted as an HWND". "Code window hosted as a WPF element" means this lead is not on the stack.

**How to confirm.** Break the UI thread while it is stuck. `SetWindowPos`, `OnWindowPositionChanged`, or `HwndHost.UpdateWindowPos` above `UpdateLayout` means the resize is synchronous again.

---

## Looked at, and not a lead

| Area | Why it dropped out |
|------|--------------------|
| Editor host metadata progress | Gear and process panel coalesce to one Background post. |
| Editor host `.Result` inside `MetadataCache` | Runs in the editor-host process. The UI thread does not wait on that type. |
| Avalonia frame copy | Posted at Background. |
| Process panel timer | One-second dispatcher timer. Default timer priority is Background. |
| Settings `JoinableTaskFactory.Run` | Off-UI pull of `settings.json`. Not a stream. |
| Code cleanup | Commands are not registered (`CodeCleanupUiEnabled` is off). |
| Snippets and templates | Registry only. |
| Stop build on first failure | One UI-thread handler, then it returns. |
| Viewport allocation loop | `PreviewerWindowImpl` ignores `ClientViewportAllocated`. `RemoteView` still sends it on size change. The host does not resize from it. |

---

## Already closed, kept here so they are not re-opened by accident

- Stopping the composition hook with a Background post. `RequestStopPump` uses Send, and `SuspendHost` calls it on the UI thread before the process is killed. A Background unhook never ran while new frames were still posting Render work.
- Leaving a preview host alive after the document frame closed. `OnClose` stops it. `WindowPane.Dispose` can run much later.
- Applying Avalonia frames with `SwitchToMainThreadAsync` (Normal) per frame.
- Synchronous `SetWindowPos` on the hosted editor during WPF layout. The `SWP_ASYNCWINDOWPOS` flag did not do this for a same-thread window. Lead 8 defers the call. Do not move it back onto `OnWindowPositionChanged`.
- A Normal post per buffer change (`ApplyTextChangedOnUiAsync`). Edits now arm the debounce timer from one Background post per burst.
- A forever `DoubleAnimation` on the status-bar gear. The gear now steps on a Background timer.

---

## When it happens again

Do not restart the stall-log loop. The trace was removed. The calls that held the UI thread were Visual Studio's, and `CopyOnUi` was not among them. Clearing the shell is moving the main window, or opening the Output window's "Show output from" dropdown and pressing the down arrow.

The missing evidence is one full UI-thread stack, taken while the shell is still stuck. Leave the window alone. From a second Visual Studio, attach to that `devenv.exe`, break all threads, and copy the main thread. A `Cornerstone.*` frame means the designer is in the call. A stack of only `Microsoft.VisualStudio.*` and WPF means the designer is not in the call, and this page stays closed.

The hosted text view is `TextEditorHost` plus `VsCodeWindowHost` in the VSIX. It is not the `Cornerstone.VisualStudio.EditorHost` process. Bobby tests a Release build in the regular instance, hive `18.0_642f1c90`, not Exp.

Two notes from the 2026-10-08 pass are not leads yet. Close on the process panel runs `PreviewerProcess.Kill` and `EditorHostSession.Shutdown` on the click thread. `ExtensionProcessLifetime` tries a second job object so one preview's children die with it. If that assignment fails, `Process.Kill` on .NET Framework 4.7.2 leaves those children until Visual Studio exits.
