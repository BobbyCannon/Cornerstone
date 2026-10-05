# How the Visual Studio extension works

Walk of the Cornerstone designer extension: what loads inside Visual Studio, how a `.cxaml` or `.axaml` document gets a preview, and how completion and the other features reach out of process.

Each section ends with a **Shell** note. That note answers one question: can this section leave Visual Studio ignoring input until a key or a window resize gets through?

Open leads from this walk are in [Todo/ShellLockup.md](Todo/ShellLockup.md). This page does not change code.

---

## Why a resize wakes the shell

`devenv` is a WPF process. Work for the UI is a queue with priorities. A higher number runs first.

| Priority | Value | What uses it here |
|----------|------:|-------------------|
| Send | 10 | One-shot unhook of the preview render pump |
| Normal | 9 | `SwitchToMainThreadAsync` (the Visual Studio default) |
| Render | 7 | Shell layout and render. The preview pump must not post here. |
| Input | 5 | Keyboard and mouse. While a preview is moving, one coalesced kick shares this band. |
| Background | 4 | Installing the preview hook, Avalonia frame copy, status-bar gear, process-panel refresh |

Keyboard and mouse sit at Input. Anything posted at Render, Normal, or Send runs before them. If that higher band keeps receiving new work, Input never runs. The IDE looks frozen: clicks and keys do nothing.

A window resize is still a Win32 message (`WM_SIZE`). The shell handles that message in the native window loop, which is why dragging the border can make the IDE respond again. A key that the native filter still sees can do the same. The backlog was never lost. The higher-priority band finally yielded.

That is the symptom this walk is checking for. A one-time post at Normal is a hitch. A stream of posts above Input is the lockup.

---

## 1. What loads inside Visual Studio

The extension is a classic Visual Studio SDK package on .NET Framework 4.7.2, plus a thin in-process VisualStudio.Extensibility entry used only for modern Settings. [ExtensibilityPlatform.md](ExtensibilityPlatform.md) records why the designer stays in-process.

`CornerstonePackage.InitializeAsync` runs when a solution exists (`ProvideAutoLoad` on `SolutionExists`, background load). On the UI thread it:

1. Opens the Cornerstone output pane and attaches Serilog.
2. Registers `EditorFactory` for `.cxaml` and `.axaml`. `.xaml` stays with the Visual Studio XAML editor.
3. Creates `SolutionService` from DTE.
4. Starts `CompletionMetadataWarmup` so the editor host can read assemblies after the solution opens and again after a build.
5. Starts the settings bridge, the stop-on-first-build-failure listener, and the View → Other Windows → Cornerstone command.
6. Injects the status-bar gear off to the side (`CornerstoneStatusBarButton`).

The package implements `IDisposable`. On dispose it shuts down the editor host and every live preview process so an upgrade can delete the extension folder.

MEF parts (completion, error tags, suggested actions, settings store) load with the editor, not from `InitializeAsync`.

**Shell.** Startup switches to the UI thread once and returns. The gear injection and metadata warm-up continue in the background. This section does not stream work above Input. The gear’s later updates are posted at Background (section 8).

---

## 2. Opening a document

`EditorFactory.CreateEditorInstance` runs on the UI thread when Visual Studio opens a `.cxaml` or `.axaml` file. It obtains the text buffer and builds an `EditorPane`.

The pane’s content is a `CornerstoneDesigner` WPF control, created in the pane constructor because Visual Studio queries `Content` immediately and treats null as a failure. The real editor (`IVsCodeWindow`) arrives later. `TextEditorHost` creates it once the text buffer is ready, then the pane finishes initialization:

- Sites the code window inside `VsCodeWindowHost`, an `HwndHost`.
- Registers the pane as the frame’s `VSFPROPID_ViewHelper` so show, hide, and close notifications arrive.
- Reads settings and starts the preview when the document is visible.

The hosted editor is a native HWND inside WPF. When WPF layout changes that HWND’s size, `CodeWindowHwndHost.OnWindowPositionChanged` calls `SetWindowPos` with `SWP_ASYNCWINDOWPOS`. The size change is posted. A synchronous `SetWindowPos` during layout sends `WM_WINDOWPOSCHANGED` back into the editor on the same stack, and the shell stays frozen until another window message arrives. That flag is the fix for that path.

The pane forwards find, status bar, toolbox, and most editor commands to the hosted code window. F12 is handled first (section 7). Quick Find asks for the real `IVsFindTarget` through `cmdidLocateFindTarget`; the pane returns the code window’s target so find stays in the document.

**Shell.** Creating the pane is UI-thread work, then it returns. The HWND size path posts. It does not wait. This section matches the resize symptom only if something else calls `SetWindowPos` without `SWP_ASYNCWINDOWPOS` during layout. The hosted editor itself does not.

---

## 3. Choosing a preview host

`.axaml` uses the Avalonia remote protocol. `.cxaml` uses the Cornerstone remote protocol (`XamlPreviewPlatformResolver`).

`CornerstoneDesigner.LoadTargetsAsync` asks the solution for executable projects that can host the document. `PreviewHostSelector` ranks them. Lower rank wins.

| Rank | Host |
|-----:|------|
| 0 | The XAML project itself, when it is an executable |
| 1 | `Cornerstone.Sample.Desktop`, for `Cornerstone.Presentation` and `Cornerstone.Presentation.*` |
| 2 | A related `*.Desktop` sibling (`Foo.Desktop` while editing `Foo` or `Foo.Controls`) |
| 3 | Any other name ending in `.Desktop` |
| 4 | A platform head: `.Windows`, `.Win`, `.Mac`, `.MacOS`, `.Linux` |
| 5 | A project whose name starts with the XAML project name |
| 6 | Any candidate that has a desktop stack |
| 7 | The startup project |
| 8 | A project that directly references the XAML project |
| 9 | Everything else |

`Microsoft.AspNetCore.SignalR.Client` does not mark a project as an ASP.NET site, so a desktop app that references it stays eligible. The designer shows a “no desktop host” error when the list is empty, and a build-required error when the chosen output is missing on disk.

The toolbar target combo is that list. Changing it recycles the preview process.

**Shell.** Target discovery uses DTE and MSBuild properties, marshalled to the UI thread where COM requires it. It runs when the document starts and after a build, then stops. It does not stream.

---

## 4. Preview process lifetime

`PreviewerProcess` is one design host per open designer. `StartHostOffUiAsync` captures the dispatcher and DPI on the UI thread, then `Process.Start`, file checks, and the handshake run on the thread pool. A 15-second handshake timeout kills a host that never connects. A 5-second send watchdog kills a host whose socket write stays in flight.

The command line is `dotnet exec` of the designer host (`Cornerstone.Designer.HostApp` for Cornerstone, the Avalonia previewer host for `.axaml`) with `--transport tcp-bson://127.0.0.1:{port}/` and the project executable. The extension listens on loopback. The host connects in.

After the handshake, the designer sends the buffer text (`UpdateXaml`). Later edits are debounced (500 ms, longer for large buffers). The same text is not sent twice. A buffer that is obviously mid-edit (a lone `<`) is held back; after about 1.5 s idle it is sent so a real error can show. Invalid markup keeps the last good frame and surfaces the host error. The overlay waits about 800 ms so a keystroke does not flash it.

The host stays up while the document is the active view. Other cases:

| Event | What happens |
|-------|----------------|
| Tab hidden, deactivated, or the window minimized | Render hook drops immediately. The process stops about 2 s later |
| Frame close | Host stops immediately (`EditorPane.OnClose` → `SuspendPreviewForClose`) |
| Source-only view | Render hook drops immediately. The process stops about 15 s later. The last error tag remains |
| Build or debug pause | Host stops and a paused banner shows |
| Build finished | Host is killed, targets reload, a new process starts so new assemblies are loaded |
| Manual refresh | Same recycle |

`SuspendHost` unhooks the render pump on the UI thread (`ReleasePreviewSurface`), then `Stop` runs off the UI thread so `WaitForExit` cannot sit on `devenv`. Closing the document does not close the code window in that path. Tearing the native editor down during frame close re-enters the shell.

Hidden tabs use the 2-second delay so a transient hide/show pair does not thrash the process. Close does not use that delay.

**Shell.** Start, stop, and XAML send are off the UI thread. Hide and Source drop the render hook immediately, then the process exits on the delay. Close removes the pump and the process together. The process can still be alive for those two seconds. It is not posting frames onto the shell.

---

## 5. How a preview frame reaches the screen

Two clients share `PreviewerProcess`. The Cornerstone client is `RemoteSession` in `Cornerstone.Presentation.Remote.Wpf`. The Avalonia client is the protocol listener inside `PreviewerProcess`.

### Host side

The Cornerstone designer host builds an off-screen window (`PreviewerWindowImpl` / `RemoteServerTopLevelImpl`) and calls `StartRendering`. Its render timer is `UiThreadRenderTimer` at 120 Hz, and the timer runs only while the compositor has work.

A paint locks a framebuffer. On unlock, `SendLastFrameIfNeeded` copies pixels into a `FrameMessage` when the framebuffer status is `Rendered`. An unchanged buffer copies nothing, so a static preview does not stream forever. The host keeps at most two frames waiting for an acknowledgement. A later paint stays local until the write finishes or an acknowledgement arrives.

`ClientViewportAllocated` from the designer is ignored by `PreviewerWindowImpl`. Applying it used to snap the preview zoom back to 100% after `ClientRenderInfo`.

### Cornerstone client (`RemoteSession`)

The socket thread copies the frame into a pending slot and calls `EnsurePump`. The first frame posts one `StartPresentOnUi` at **Background**, which subscribes `CompositionTarget.Rendering`. Later frames do not each post. The pass copies the latest pending frame into a `WriteableBitmap`, then posts **one** coalesced kick at **Input**. That kick dirties the bitmap so the next pass runs. A second frame does not queue another kick while one is waiting.

The hook stays up for 200 ms after the last painted frame, then drops. Host frames arrive between WPF passes, so the queue is often empty at the pass even while the preview is moving. Dropping the hook on that first empty pass sent every following frame back through the Background queue. That queue is shared with the rest of the shell, so a live preview paused and then drew several frames at once.

A post per frame at Render filled the band above the keyboard. Input then waited until a resize. The kick is one item at Input, not a Render item. Pixel copies still happen on the render pass.

The acknowledgement (`FrameReceivedMessage`) is sent on the socket thread as soon as the frame is copied, before the UI thread paints it.

`RequestStopPump` sets a flag, drops the pending frame, and posts `StopPumpOnUi` at **Send** so the unhook runs before a queued present. `SuspendHost` calls this on the UI thread before the process is killed. `ListenLoopback` clears that flag. Recycle listens again on the same session, and a pump left stopped would acknowledge frames and never show them.

`SuspendPump` unhooks without stopping the session. `CornerstoneDesigner.UpdatePreviewPump` calls it while the tab is hidden or the view is Source. Showing Design or Split calls `ResumePump`. The hidden-tab process still waits about 2 seconds before it is killed. The render hook does not wait with it.

When the bitmap instance changes (first frame, or a new size), `FrameReceived` is posted at Background, not raised on the render stack:

- `RemoteView.ApplyBitmap` assigns `Image.Source` and, on a size change, sets `Image.Width` and `Image.Height`.
- `PreviewerProcess` forwards the event. `CornerstonePreviewer.Update` sets the surface size and `PreviewGrid.Margin`.
- `CornerstoneDesigner.FrameReceived` applies zoom on the first frame and clears the error overlay.

Same-size frames raise `FramePainted` on the render pass only. That updates the process-panel text. It does not measure the designer or the hosted editor.

### Avalonia client

`ConnectionMessageReceived` keeps only the latest frame, acknowledges a dropped one, and copies pixels with `dispatcher.InvokeAsync` at **Background**. `CornerstonePreviewer.Update` from a background thread also posts at Background. There is no `CompositionTarget.Rendering` hook on this path.

### What the picture is

The preview control shows `RemoteView` for Cornerstone and a WPF `Image` for Avalonia. Pointer events on the image are forwarded to the host (move events smaller than half a pixel are dropped). Ctrl+wheel changes the designer zoom. The zoom box is a list of percentages. Fit modes were removed because a viewport resize and a host scale chased each other.

**Shell.** Cornerstone frames no longer post a Render-priority item per frame, and a size change no longer lays out inside the render pass. A hidden tab and Source view drop the hook immediately. The host can still paint every tick while Design or Split is on screen. That is one WPF render pass per frame, the same shape as an animation, and it yields to input between frames. The acknowledgement is still sent before the UI copy, so a busy preview can stay at the host's paint rate. That remaining rate is lead 3 in [Todo/ShellLockup.md](Todo/ShellLockup.md).

The Avalonia copy is at Background, and it is skipped while the surface is suspended.

---

## 6. Designer chrome

`CornerstoneDesigner` is the document surface: toolbar, preview, splitter, and the hosted source.

The view is Split, Design, or Source. Split uses one Absolute pane and one Star pane. Two Star panes make the WPF `GridSplitter` cancel the drag when the preview’s scroll margins change the total, which is why the splitter used to stick at half after the first frame.

The toolbar sends theme (Light / Dark / Default), color, and density with each XAML update. Those choices are shared by open designers for the Visual Studio session. Zoom is a percentage, default 100%. Ctrl+wheel can land between list values; the combo then has no selection until the zoom matches a listed level.

Build and debug set `IsPaused`. The preview stops and a banner replaces it. `OnBuildCompletedAsync` clears the pause, reloads targets, and starts a fresh host.

The error tagger (`XamlErrorTagger`) keeps the last host exception for the Error List and the squiggle. Tag and Error List updates are marshalled to the UI thread. A tag update off the UI thread can deadlock the editor.

**Shell.** Theme, zoom, and view changes are UI clicks, then a XAML send off-thread. `ApplyTextChangedOnUiAsync` uses `SwitchToMainThreadAsync` (Normal, above Input) once per buffer change to arm the debounce timer. That is one post per edit, then the timer. It becomes a problem only if buffer changes never stop. Build completion uses the same switch once per build. The error tagger uses it when applying tags. None of these are a frame stream. They are listed in the lockup todo so a later frame-pump fix does not assume the shell is clear.

---

## 7. Editor host, completion, and navigation

Completion, Enter indent, Go to Definition, grid-definition lightbulbs, and the missing-xmlns lightbulb run in one net10 process per Visual Studio instance: `Cornerstone.VisualStudio.EditorHost`. `EditorHostSession` starts it with `dotnet exec` and talks over the protocol in `Cornerstone.VisualStudio.Protocol`. The type’s own contract is that the UI thread must not wait on it.

The host loads assembly metadata from the paths the designer already resolved, caches them under the solution’s `.cscache` folder, and re-reads a file when its size or write time changes. `CompletionMetadataWarmup` asks for that load when the solution opens and after a build. Progress is reported through `EditorHostActivity`. The status-bar gear and the process panel subscribe. Both post their UI update at Background, and only one update is queued at a time. A Normal post per assembly used to sit above the keyboard for the whole metadata load. That path is closed.

### Completion

`XamlCompletionSource` and `XamlCompletionCommandHandler` are MEF exports on the designer’s text view. The session asks the host asynchronously and fills the list when the response arrives. The command handler does not block the key. Element commit uses `ICustomCommit` (one replace, one caret move). Enter and Tab are swallowed only when a session is actually committing. Leaf controls commit self-closing. Containers commit an open and close tag with the caret between them.

Enter with no completion session calls `BuildEnterIndentAsync` and applies the result when it returns. The command itself returns immediately. The indent rules are in [Editor.md](Editor.md). What the list contains is in [AutoComplete.md](AutoComplete.md).

Quote characters skip a quote that is already at the caret, then reopen value completion when the caret is in a non-text attribute value.

### Go to Definition

F12 on the designer buffer goes to `XamlGoToDefinitionCommand`, then the editor host, then `XamlGoToDefinitionNavigator`. The navigator records the caret on Visual Studio’s back stack and opens the source. Behavior by caret position is in [GoToDefinition.md](GoToDefinition.md).

If the host returns nothing useful, `HandOff` calls `JoinableTaskFactory.Run` and then the normal Visual Studio command. `Run` blocks the calling thread until that async method finishes, and on the UI thread it pumps.

### Lightbulbs

`SuggestedActionsSource` asks the editor host for a missing xmlns or alias, and for converting `Grid` column and row definitions between the attribute form and the element form. Visual Studio only inserts the edit.

**Shell.** The host process can be slow. The UI thread does not wait on completion or Enter, so a slow host delays the list and does not freeze the shell for the host timeout. Go to Definition’s hand-off does wait, on the UI thread, via `JoinableTaskFactory.Run`. That is a block for the length of the hand-off. It is a different shape from the frame pump (section 5). Metadata progress UI is at Background.

---

## 8. The other features

### Settings

`CornerstoneExtension` is an in-process VisualStudio.Extensibility contribution. `CornerstoneSettingDefinitions` publishes the Cornerstone options page. `CornerstoneSettingsBridge` copies those values into the MEF `ICornerstoneSettings` store the designer already reads, and writes changes back. The bridge can call `JoinableTaskFactory.Run` when a pull happens off the UI thread. On the UI thread it reads `settings.json` directly.

Options the designer honors include log verbosity, the preview-running tab prefix (default off), zoom, split orientation, whether panes are swapped, and stop-on-first-build-failure.

**Shell.** A settings pull is rare. `Run` off the UI thread blocks that caller until the UI thread applies the file. It does not post a stream above Input.

### Process panel and status bar

View → Other Windows → Cornerstone, and the 32px gear at the right of the status bar, open `CornerstoneProcessesWindow`. One row is the editor host. Each open designer adds a preview row (state, document name, activity such as “XAML sent” or a frame size). The list does not select a row. A running row has Close, which calls `PreviewerProcess.Kill` and leaves the designer stopped instead of showing a crash banner.

The panel refreshes on a 1-second `DispatcherTimer` and when `EditorHostActivity` changes. Activity updates are one Background post at a time.

**Shell.** One-second refresh and coalesced Background posts sit below Input. This section matched the lockup only in the older form, where each metadata tick posted above Input.

### Stop the solution build on first failure

`StopBuildOnFirstFailureService` listens to DTE build events. When a project fails and the option is on (default), the rest of the solution build is cancelled. Projects that have already started may still finish. The listener keeps its own reference to `BuildEvents` because DTE does not root it.

**Shell.** The handler runs on the UI thread when DTE raises it, then returns. It does not wait on the build.

### Code cleanup

`CodeCleanupService` and the commands under Tools and Solution Explorer format `.cxaml` and `.axaml` (whitespace, newlines, XML structure, xmlns and attribute order, self-closing tags). The command registration is behind `CornerstoneConstants.CodeCleanupUiEnabled` and is currently off, so the menu entries are not installed. The service code remains.

**Shell.** Cleanup marshals to the UI thread to edit buffers. A batch over a solution would occupy the UI thread for the edits. It is not on the preview frame path, and the commands are not wired up.

### Snippets and templates

`Snippets/snippets.pkgdef` and `Templates.pkgdef` install Avalonia C# snippets and the Cornerstone and Avalonia project templates. They are registry contributions. They do not run code when a designer is open.

**Shell.** None after install.

---

## 9. Shutdown

`CornerstonePackage.Dispose` calls `EditorHostSession.Shutdown` and `PreviewerProcess.ShutdownAll`. `ExtensionProcessLifetime` tracks preview processes so a hard close of Visual Studio still has a handle to kill. A preview that survives `devenv` keeps the extension folder locked, and the next install leaves the old folder beside the new one.

Per document, close is earlier than package dispose: `OnClose` stops that document’s host and its render hook without waiting for `WindowPane.Dispose`.

**Shell.** Shutdown must run `RequestStopPump` on the UI thread before the process dies. `SuspendHost` does that. A host that is still sending frames, with the composition hook still attached, is the leftover-tab case described in section 5. Close is written to prevent it. A document that never received `OnClose` (frame helper not set) would still hit it. `AdviseWindowFrameNotifications` sets the helper during pane init, and the pane also implements `IVsWindowFrameNotify3` so the shell can query the interface directly.
