# Shell lockup — leads

Symptom: Visual Studio stops taking input, and a key or a window resize makes it respond again.

The walk is [../HowItWorks.md](../HowItWorks.md). Leads 1, 2, and 4 are addressed in `RemoteSession` and `CornerstoneDesigner`. Leads 3, 5, 6, and 7 are still open.

WPF dispatcher order, highest first: Send (10), Normal (9), Render (7), Input (5), Background (4). Keyboard and mouse run at Input. Work posted above Input runs first. A stream of that work leaves Input sitting in the queue. `WM_SIZE` is still handled as a Win32 message, which is why a resize wakes the shell.

---

## 1. Cornerstone frames are posted at Render

**Addressed.** `EnsurePump` posts one `StartPresentOnUi` at Background, and only when the render hook is not already up. Later frames wait on that hook. While it is up, one coalesced kick at Input asks for the next pass. The hook stays up for 200 ms after the last painted frame so a live preview does not reinstall it through the Background queue on every frame (that pause-then-burst is not the lockup; it is the jitter). A Render post per frame was the stream above Input. Do not remove the hold, and do not move the kick to Render.

**Was.** `RemoteSession.EnsurePump` posted `StartPresentOnUi` at `DispatcherPriority.Render` for every frame copied on the socket thread.

`Cornerstone.Presentation.Remote.Wpf/RemoteSession.cs`

Render is above Input. `StartPresentOnUi` subscribes `CompositionTarget.Rendering` and copies the frame inside that callback. While new frames arrive, the Render band keeps receiving work, and Input does not run.

The Avalonia path does not do this. `PreviewerProcess.ApplyFrameOnUiAsync` copies at Background. The status bar and the process panel also post at Background. The release note that says frame work stays below input describes those paths. It does not describe `EnsurePump`.

**When it would match the symptom.** A preview that keeps producing frames: animation, a blinking caret, a clock, or a compositor that never drops its render-loop item. The designer host arms `UiThreadRenderTimer` at 120 Hz while the compositor has work (`Cornerstone.Designer.HostApp` / `UiThreadRenderTimer`). A static preview should stop. `Framebuffer.CopyRendered` returns null until a new paint marks the buffer `Rendered`.

**When it would not.** One frame after an edit. A handful of Render posts drain, then Input runs. That is a hitch, not a lockup that waits for a resize.

**How to confirm.** Reproduce with a `.cxaml` whose preview invalidates every tick, and again with a static page. While it is stuck, break on the UI thread and see whether `OnCompositionRendering` / `StartPresentOnUi` are still queued. A static page that still locks points at lead 2 or lead 4 instead.

---

## 2. Size changes run layout inside the render pass

**Addressed.** `FrameReceived` is posted at Background. `RemoteView` and `CornerstonePreviewer` size the surface there, below Input, not inside `OnCompositionRendering`.

**Was.** `ProcessPendingFrameOnUi` ran from `OnCompositionRendering` and raised `FrameReceived` on that stack when the bitmap instance changed (first frame, or a new width or height).

Subscribers then run before the render pass returns:

- `RemoteView.ApplyBitmap` sets `Image.Width` and `Image.Height` when the size differs.
- `PreviewerProcess.OnRemoteFrameReceived` forwards to `CornerstonePreviewer.Update`. `Update` calls `ApplyProcessBitmap` immediately when it is already on the UI thread, and that sets the surface size and `PreviewGrid.Margin`.

That is layout during `CompositionTarget.Rendering`. The hosted editor sizes itself with `SetWindowPos` and `SWP_ASYNCWINDOWPOS` (`VsCodeWindowHost`), so the old synchronous size message into the editor is closed. The layout property changes are still on the render stack.

**When it would match.** Opening a designer (the first real frame replaces the empty bitmap), changing zoom, or a DPI change. The shell sticks until a resize forces a new layout pass.

**How to confirm.** Stuck immediately on first paint of a static page, and a same-size frame later does not stick. Break in `RemoteView.ApplyBitmap` and see `CompositionTarget.Rendering` on the stack.

---

## 3. The frame acknowledgement does not wait for the UI

**Still open.** Lead 1 no longer posts per frame, so this no longer fills a Render queue. The host can still paint every tick while the hook is up.

**Was an amplifier for lead 1.** `RemoteSession.OnMessage` sends `FrameReceivedMessage` as soon as the bytes are copied, on the socket thread. The host allows two unacknowledged frames (`RemoteServerTopLevelImpl.MaxFramesAwaitingAck`) and sends again when the acknowledgement arrives or the socket write completes.

The cap limits how many frames sit on the wire. It does not limit how fast the next frame is produced once the acknowledgement is back. If the host paints every tick, Visual Studio’s Render queue refills without waiting for the previous copy to finish.

**How to confirm.** Same setup as lead 1. If acknowledgements were delayed until after `ProcessPendingFrameOnUi`, the host would slow to the UI thread. That experiment is a behavior change, not something this note applies.

---

## 4. A hidden document keeps painting for about 2 seconds

**Addressed.** `UpdatePreviewPump` calls `SuspendPreviewSurface` as soon as the tab is hidden or the view is Source. The process still exits on the 2-second (hide) or 15-second (source) timer. The render hook does not stay up for that wait. Showing Design or Split calls `ResumePreviewSurface`.

**Was.** `CornerstoneDesigner` used `BackgroundStopDelay` (2 seconds) after hide, deactivate, or minimize. `SuspendHost` ran when that timer ticked. Until then the host was alive and lead 1 still applied.

Close is different. `EditorPane.OnClose` calls `SuspendPreviewForClose`, which unhooks the pump immediately and stops the process off the UI thread.

**When it would match.** Switching away from a busy preview, or minimizing, and the shell sticks for a short time or until a resize. After the 2 seconds the pump should drop. A stick that lasts with no designer visible, and no preview process left, is not this lead.

**How to confirm.** Process panel (View → Other Windows → Cornerstone) after the stick. A preview row still Running, for a document that is not the active tab, supports this. No preview row, and the editor host Idle, points away from the frame pump.

---

## 5. `SwitchToMainThreadAsync` is still above Input

**Watched, weaker.** The Visual Studio helper posts at Normal. These call sites use it:

- `CornerstoneDesigner.ApplyTextChangedOnUiAsync` (arm the debounce timer)
- `OnBuildCompletedAsync` and the error-overlay helpers
- `EditorPane.UpdatePreviewLiveTabCaptionAsync`
- `XamlGoToDefinitionCommand` / `XamlGoToDefinitionNavigator` after the host returns
- `XamlErrorTagger` when applying tags
- `CornerstoneSettingsBridge` pulls

Each call is one post, then the method returns. That drains. It matches a resize-only wake-up only if the posts never stop (a buffer-change loop, or a tagger that invalidates itself).

The frame, gear, and process-panel paths were moved to Background specifically because Normal starved input. These call sites were left on the helper. They are recorded so a fix for lead 1 does not treat them as already safe under a storm of edits.

---

## 6. Go to Definition hand-off blocks the UI thread

**Different shape.** `XamlGoToDefinitionCommand.HandOff` calls `ThreadHelper.JoinableTaskFactory.Run` and then the normal Visual Studio command. `Run` does not return until the async method finishes. On the UI thread it pumps.

That can stall the shell for the length of the hand-off. It is not the “frames keep the Render band full” mechanism. Track it if the lockup is specifically F12, including F12 that falls through to Visual Studio.

Completion and Enter do not wait. `XamlCompletionCommandHandler` returns as soon as the host request is started. A slow editor host delays the list. It does not hold the UI thread for the host timeout.

---

## 7. Verbose logging and the output pane

**Conditional.** `OutputPaneEventSink.Emit` calls `IVsOutputWindowPane.OutputStringThreadSafe` for every event the logger accepts. Default verbosity is Information. Per-frame lines are Verbose. Information logs on a frame only for the first paint and for a size change (`PreviewerProcess.NoteFramePainted`).

If the Cornerstone log option is raised to Verbose, a frame stream becomes an output-pane stream as well. Confirm only when the option is above Information.

---

## Looked at, and not a lead

| Area | Why it dropped out |
|------|--------------------|
| Editor HWND size | `SWP_ASYNCWINDOWPOS` is set. A synchronous `SetWindowPos` during layout was the older form of this bug. |
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
- Synchronous `SetWindowPos` on the hosted editor during WPF layout.
