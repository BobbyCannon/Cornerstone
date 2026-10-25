# Presentation performance audit

Read-only audit of the `Cornerstone.Presentation` UI-thread hot path on 2026-10-07. No runtime code was changed. Use this to pick the next change.

Out of scope: Keystone and AppDispatcher projection ([../AppDispatcher.md](../AppDispatcher.md)), and the Welcome-card / Gamepad browser lockups. Those were exceptions escaping pointer-up, not layout cost. Trace the next browser lockup with [BrowserDebug.md](BrowserDebug.md) before changing the dispatcher.

## Decision list

Pick one. Each item is a candidate, not a committed change.

| # | Candidate | Why it is next | Risk |
|---|-----------|----------------|------|
| 1 | Turn on region dirty-rect clipping and measure a real window | Skia supports regions. The option is off, so every frame uses one union rect and the compositor skips per-visual dirty tests. Confirm the swapchain retains the previous frame first; if it does not, the target already forces a full-surface repaint | Visual diffs. `UseSaveLayerRootClip` exists for a known Skia bug |
| 2 | Skip text layout rebuilds that repeat the same constraint | `TextBlock.ArrangeOverride` rebuilds `TextLayout` on every arrange. `TextPresenter.MeasureOverride` rebuilds it on every measure. Shaped runs can be cached; line breaking still runs | Caret and alignment if the skip is wrong |
| 3 | Batch `ChannelControl` cell draws | Each data tick records two draw calls per queue slot on the UI thread. Measure is already throttled | Only hosts that still show `ChannelControl`. The sample tab is gone |
| 4 | Keep `ItemsControl` on `StackPanel`; put long lists on `ListBox` or a `VirtualizingStackPanel` panel | The default panel realizes every item. Changing the default would change measure of existing screens | A default-panel change is a behavior break. Prefer the call site |
| 5 | Move `DispatcherPriority` into a static constructor | Field initializers still form the chain. A reorder tool has collapsed this before and blanked windows | None at runtime until something reorders the fields |

Do not time-slice `ExecuteJobsCore`. Do not add a permanent render-priority frame pump. Both were tried and reverted. See [Rejected](#rejected).

## How a frame runs

1. A property or explicit call invalidates measure, arrange, or the visual.
2. `LayoutManager.QueueLayoutPass` schedules one `MediaContext.BeginInvokeOnRender` callback. Further invalidations in that window only enqueue controls.
3. `MediaContext.ScheduleRender` posts one dispatcher job. Layout asks for `DispatcherPriority.Render`. The animation timer asks without forcing that priority.
4. The dispatcher runs every pending job above `Input` before it looks at input. That includes `Default`, `Loaded`, `Render`, `Normal`, and `Send`.
5. The render job pulses the clock, runs layout callbacks, then commits compositors.
6. The commit, still on the UI thread, records a draw list for each dirty visual (`visual.Render`) and serializes the batch.
7. The compositor thread rasterizes. The UI thread does not paint pixels.
8. When the batch finishes, completion posts back at `DispatcherPriority.Send`. If the clock still has subscriptions or another commit was requested, the next frame is scheduled.

`PresentationSource` constructs `CompositingRenderer`. `ImmediateRenderer` is not the window path.

## Priority

Higher `Value` runs first. The chain is field initializers in `Cornerstone/Cornerstone/Presentation/DispatcherPriority.cs`, not a static constructor. Textual order today:

| Priority | Value | Relative to input |
|----------|------:|-------------------|
| Send | 9 | Above. Not sliced |
| Normal | 8 | Above. Not sliced |
| Render | 4 | Above. Not sliced |
| AfterRender | 3 | Above. Not sliced |
| UiThreadRender | 2 | Above. Not sliced |
| Loaded | 1 | Above. Not sliced |
| Default | 0 | Above. Not sliced. `Dispatch` / `Post` default |
| Input | -1 | Yield boundary |
| Background | -2 | Yields |
| SystemIdle | -5 | Yields. `MinimumActiveValue` |

`ExecuteJobsCore` (`Threading/Dispatcher.Queue.cs`) executes `job.Priority > Input` with no time budget and no pending-input check. A job that is already running is never preempted. A heavy first measure holds the thread until it returns.

Jobs at or below `Input` yield. Jobs at `Default` and above do not.

- On the signal turn, a platform with explicit background processing (`BrowserSingleThreadedDispatcherImpl`) returns to the browser without starting the first `Input`-or-lower job. The background callback then runs that band for up to 50 ms (`ExecuteJobsCore(true)`).
- The same 50 ms fallback applies when the platform has neither a pending-input query nor explicit background processing.
- A platform that can query pending input stops the `Input`-or-lower band when input is waiting. It still does not slice `Default` and above.
- `DispatcherOptions.InputStarvationTimeout` defaults to 1 second. `MediaContext` stores it as `MaxSecondsWithoutInput`. It does not slice the 50 ms path. It demotes the next render job to `Input` when the input marker has been waiting longer than that and the caller did not pass `now: true`. The marker means an `Input`-priority dispatcher job has run. It does not mean the browser has a pointer event waiting.
- `Dispatcher.Invoke` defaults to `Send` and, on the UI thread, runs inline. `Post`, `InvokeAsync`, and `Dispatch` default to `Default`, which is still above `Input`.
- `DispatcherOperation.Wait` on the UI thread calls `RunJobs`. That drain ignores pending OS events. Single-thread browser does not implement `IControlledDispatcherImpl`, so `SupportsRunLoops` is false and every UI-thread wait takes this path.

`Loaded`'s comment says the job runs after layout and render. The values do not. `Render` is 4 and `Loaded` is 1, so a queued render runs before `Loaded`. Both run before `Input`. Layout does not post at `Loaded`.

`BeginInvokeOnRender` calls `ScheduleRender(true)`. If a render job is already queued, that call sets its priority back to `Render`. Layout therefore undoes a starvation demotion. The 16 ms animation timer calls `ScheduleRender(false)`, so only that path can sit at `Input` after a starved second.

## What already coalesces

- `Layoutable.InvalidateMeasure` returns immediately when `IsMeasureValid` is already false. The first call also clears arrange and calls `InvalidateVisual`.
- `LayoutManager.QueueLayoutPass` schedules only when `!_queued && !_running`.
- `LayoutQueue` drops a second enqueue of the same control while it is already active, and stops enqueueing a control after `MaxPasses` (10) times in one pass. The 11th is logged as a layout cycle and retried once on the next pass.
- `MediaContext` keeps a single `_nextRenderOp`. A second schedule does not post a second job.
- `CompositingRenderer.AddDirty` unions into `_dirty` and queues one composition update.
- `CommitCompositorsWithThrottling` does not send a new batch while one is in flight. The animation timer is not restarted in that case. Completion reschedules.
- `ChannelControl` throttles `DataChanged` to about 16 ms and calls `InvalidateVisual`, not `InvalidateMeasure`. Grid size does not change when only the heads move.
- `TextBlock` keeps a `TextRunCache` across arrange. Content changes call `InvalidateTextLayout`, which drops the cache. Wrapping and alignment changes keep it.
- `ListBox` and `ComboBox` default their panel to `VirtualizingStackPanel`. `TreeDataGrid.UseFixedRowHeight` defaults to true so scroll extent does not depend on per-row estimates.
- Binding `PublishValue` calls the sink directly on the UI thread. The closure and `Post` exist only for an off-thread publish.
- Browser wake-up does not use `isInputPending`. `BrowserSingleThreadedDispatcherImpl` says gating on that API starves low-priority jobs for as long as the pointer moves. Order is `scheduler.postTask`, then `MessageChannel`, then `setTimeout`. Source: `Platforms/Browser/webapp/modules/cornerstone/singleThreadedDispatcher.ts`.

## Findings

### 1. Region dirty rects are off

`CompositionOptions.UseRegionDirtyRectClipping` has no default of true. `ServerCompositionTarget` builds a `MultiDirtyRectTracker` or `RegionDirtyRectTracker` only when the platform supports regions and the option is exactly `true`. Otherwise it uses `SingleDirtyRectTracker`, which unions every dirty rect into one.

Skia reports `SupportsRegions => true` (`Backends/Skia/PlatformRenderInterface.cs`). Headless reports false.

`ServerCompositionVisual.RenderContext` then does this for the single-rect tracker: it clips to the union, then sets the tracker reference to null. The per-visual `Intersects` test is skipped. Visuals that miss the clip are still skipped. Scattered invalidations that union into a large rect walk and repaint that whole union.

`MaxDirtyRects` defaults to 8 when the option is on. `DirtyRectMergeEagerness` defaults to 1000 (WPF uses 50000; the property comment says the Cornerstone value is unstable). `UseSaveLayerRootClip` is the workaround for Skia issue 327877721.

Label: PerFrame. Candidate 1.

### 2. Scene recording is on the UI thread

`Compositor.CommitCore` requires the UI thread. It runs every `RequestCompositionUpdate` callback before serializing. `CompositingRenderer.UpdateCore` calls `visual.Render` for each dirty visual and stores the draw list. A control whose `Render` walks a large model blocks input inside the frame, on top of layout.

`AddDirty` and `RecalculateChildren` throw `InvalidOperationException` ("Visual was invalidated during the render pass") when `_updating` is true. Do not invalidate from inside `Render`.

Label: BlocksInput when `Render` is heavy. The compositor thread is not the place to look first.

### 3. Text layout is rebuilt more often than the text changes

`TextLayout` formats in the constructor (`CreateTextLines`). There is no line virtualization. A `TextRunCache` keeps shaped runs when only the width changes. The common case is one inline entry; a second key allocates a dictionary.

`TextBlock.MeasureOverride` disposes the layout only when the deflated constraint changes, or when an embedded control reports a new size. `ArrangeOverride` always disposes and rebuilds. `OnMeasureInvalidated` also disposes, because a second invalidation before the next measure would keep the old text. `Render` draws the existing layout.

`TextPresenter.MeasureOverride` disposes and rebuilds on every measure, then invalidates arrange. `ArrangeOverride` rebuilds only when the final width is not close to the measure constraint. The caret timer calls `InvalidateVisual` only.

`TextFormatterImpl.FormatLine` skips shaping on a cache hit. The layout object and the line walk still run.

Label: PerFrame for a presenter that measures every pass, and for a `TextBlock` that arranges every pass. Candidate 2. Parents often arrange to a smaller rect than the measure constraint, so a skip must compare the constraint the live layout was built with.

### 4. `ItemsControl` realizes every item

`ItemsControl.DefaultPanel` is `StackPanel`. `ListBox.DefaultPanel` and `ComboBox` use `VirtualizingStackPanel`.

`ScrollContentPresenter.MeasureOverride` measures the child with infinity on each axis that can scroll. `StackPanel` then measures every child. `VirtualizingStackPanel.MeasureOverride` realizes from the extended effective viewport, not from that infinite constraint.

`ScrollContentPresenter` overrides `ClipToBounds` to true. `LayoutManager.CalculateEffectiveViewport` starts at infinity, replaces that with the root bounds, and intersects ancestors only when `ClipToBounds` is true. A virtualizing panel inside a scroll presenter therefore gets a finite viewport. A `StackPanel` does not consult that viewport.

A `ListBox` inside another scroll viewer that gives the list infinite height grows to its content. The inner viewport then covers every row, and virtualization realizes them all. Give that list a finite height.

`VirtualizingStackPanel` recycles by `RecycleKey` into a stack. An item that is its own container is hidden, not pooled. The first realization still creates containers. An item that is its own container is hidden on recycle and stays a child. It is not pooled.

`TreeDataGridPresenterBase` uses the same realize/recycle pattern. A 0×0 effective viewport is stored as an invalid viewport so measure estimates instead of realizing nothing. The comment in that method calls the 0×0 event a layout bug.

`CacheLength` on `VirtualizingStackPanel` defaults to 0, so there is no extra buffer of realized rows.

Label: FirstLayout and Scroll. Candidate 4.

### 5. `ChannelControl` draws every slot

`DrawQueue` loops `0 .. Capacity` and calls `FillRectangle` then `DrawRectangle` for each cell. `InvalidateFromData` posts `InvalidateVisual` through a `Throttle` of `1000/60` ms. `MeasureOverride` caches `LayoutPlan` from capacity and usable width.

Label: PerFrame, UI-thread recording. Candidate 3.

### 6. One layout pass can re-enter

`LayoutManager.MaxPasses` is 10 for the outer pass and again inside `InnerLayoutPass`. `Measure` walks to the root first. A control whose desired size changes calls `ChildDesiredSizeChanged`, which invalidates the parent when the parent is not the control currently measuring. That is a later pass in the same layout, not a new frame.

`RaiseEffectiveViewportChanged` runs after the inner pass. A viewport change that invalidates measure continues the outer loop. Bring-into-view runs after the viewport is stable and can invalidate again.

`MediaContext.FireInvokeOnRenderCallbacks` throws `InvalidOperationException` ("Infinite layout loop detected") after 153 callback waves. `LayoutQueue` is the earlier, softer guard.

The root visual is measured with `Size.Infinity`.

`ExecuteInitialLayoutPass` measures and arranges the root, then runs a full pass because the first scrollbar guess can be wrong. Root measure uses `Size.Infinity`.

`Grid` remeasures star and auto groups while desired size changes, and stops at `c_layoutLoopMaxCount` (5).

`LayoutQueue.EndLoop` puts a control that hit the per-pass cap back on the queue and does not call `QueueLayoutPass`. Those controls wait for a later invalidation.

A re-entrant `ExecuteLayoutPass` while bring-into-view is running does up to 10 inline inner passes and returns without raising `LayoutUpdated`. Any other re-entry while `_running` is true skips measure and arrange, clears `_queued`, and raises `LayoutUpdated`. `VirtualizingStackPanel` calls `ExecuteLayoutPass` directly from scroll-into-view. That call has to land outside an enclosing pass, or the scroll measure is skipped.

Label: FirstLayout. Already bounded. The cost of one pass is still unbounded.

### 7. Render completion is posted at Send

`RequestCompositionBatchCommitAsync` posts `_triggerCommitRequested` at `DispatcherPriority.Send` when a batch is already pending. `CompositionBatchFinished` also posts at `Send`. Send is the top priority, so a completion runs before input and before the next normal render job. One completion per frame is the steady state. A handler that invalidates again from that completion extends the burst above input.

Label: BlocksInput only if completion keeps scheduling more Send work.

### 8. Browser clock stays armed on purpose

`BrowserThreads` is an empty `ContentControl`. Its theme (`Theme/Controls/BrowserThreads.cxaml`) runs a one-minute infinite alternate animation that sets `BorderBrush` to `Transparent` at both keyframes. That subscription is what keeps `MediaContext` scheduling the ~16 ms render timer. `RenderCore` starts `_animationsTimer` only when a commit is not already in flight and the clock has subscriptions. The timer stops itself and calls `ScheduleRender(false)`.

Removing the subscription makes browser scrolling, hover, caret, and animation hitch. Do not replace it with a job that stays queued at `Render` for the whole time a view is open.

Label: AlreadyMitigated. The armed timer means a render job is usually ahead of the next click. That is acceptable while each frame returns.

The compositor has a second clock. `ServerCompositor.RenderCore` returns false, and the render timer pauses, when there is no animation tick, the last commit is at least `CommitGraceTicks` (10) old, and no target is waiting on a render target that will not wake the loop itself. Single-thread browser starts `requestAnimationFrame` once, from `BrowserRenderTimer.StartOnThisThread`, and `timer.ts` always schedules the next frame. Setting `Tick` to null makes `RenderFrameCallback` return without a managed render. The JavaScript loop stays. Do not add a second loop.

### 9. A paint walks the whole composition tree once, then may prune

`RenderRootToContextWithClip` calls `StampAirspaceTreeOrder` before `root.Render`. The stamp recurses every child. It runs on every paint, including a frame with no native holes.

The draw walk then skips a subtree that misses the clip, has opacity at or below 0.003, or fails a dirty-rect test. With `SingleDirtyRectTracker` the tracker is cleared and only the union clip remains, so the draw prune is that one rect.

`ServerCompositionTarget.Update` visits a child only when the subtree is dirty for render or needs a bounding-box update. The update walk is not a full walk on a clean frame. The stamp is.

A full-surface repaint is forced when `_fullRedrawRequested` is set, or when the target is not drawing through a layer and `PreviousFrameIsRetained` is false. Layer recreate (size change or a corrupted layer) also sets `fullRedraw`. `RequestFullRedraw` only sets `_redrawRequested`. It does not set `_fullRedrawRequested`. If the dirty set is empty and the previous frame is retained, that call enters `Render` and then skips the draw.

Label: PerFrame on the compositor. On single-thread browser that callback is the only thread, so a large stamp plus a full repaint delays the next input turn. Candidate 1 depends on the swapchain retaining frames. If it does not, region rects never get a chance to clip.

### 10. Dispatcher priority initializers

`DispatcherPriority` values are `static readonly` fields that call each other (`Input = new(Default - 1)`, and so on). C# runs those initializers in textual order. A cleanup that reorders the fields can give two priorities the same value. That has blanked windows and collapsed toolbar hover previews. The safe shape is one static constructor that assigns the chain in dependency order.

Label: latent, not a steady-state cost. Candidate 5.

## Smaller costs

These are real and not the first thing to change.

- `Dispatcher.Post` constructs `DispatcherOperation` with `captureExecutionContext` left at true, so each post calls `ExecutionContext.Capture`. `ScheduleRender` and `DispatcherTimer.Restart` pass false. A hot `Dispatch` still pays the capture. `ChannelControl` is throttled, so that path is about one post per frame.
- `MediaContextClock.Pulse` copies `_observers.ToArray()` on every pulse. `BrowserThreads` keeps a subscriber, so the copy happens on every animation frame. Commit copies `_requestedCommits.ToArray()` the same way.
- An effective property change allocates `PresentationPropertyChangedEventArgs<T>` and, for `INotifyPropertyChanged`, a `PropertyChangedEventArgs`.
- `DispatcherTimer.Restart` holds `_instanceLock` while it calls `InvokeAsyncImpl` and `AddTimer`, and those take `Dispatcher.InstanceLock`. `PromoteTimers` takes `InstanceLock` first, drops it, then `Promote` takes `_instanceLock` and sets `Priority`, which takes `InstanceLock` again. A worker thread inside `Restart` can deadlock the UI thread inside `PromoteTimers`. The timer comment allows creation from a worker. Do not start, stop, or change the interval off the UI thread on WASM.

## Rejected

Do not retry these for a browser lockup or a slow frame:

- A frame pump that queues render work for the whole time a view is open. Render sits above input, so the click queue does not run.
- Yielding inside `ExecuteJobsCore` after about 50 ms for `Render` and `Send` jobs. It did not stop the Welcome-card lockup.
- Posting the Welcome tab change at `DispatcherPriority.Background`. It only delayed the same lockup.
- `isInputPending` gating on the browser dispatcher.

The Welcome-card failure that was traced was `BrowserInteropProxy` construction throwing out of pointer-up. The Gamepad failure was a trimmed `WeakEventManager` event, then a cross-thread `lock` on an 8 ms `System.Threading.Timer`.

## Not line-audited

Style selector reevaluation, the glyph atlas, and hit-test cost beyond `CompositingRenderer.HitTest` (that path uses the composition tree). `StackPanel` was confirmed to measure every child at infinite height on the stack axis. `DockPanel` and the rest of `Grid.MeasureOverride` were not read past the star/auto loop cap. Ancestor measure skipping is in `LayoutManager.Measure`: a valid ancestor is not measured again. Whether the desktop swapchain sets `RetainsPreviousFrameContents` was not read.

## File map

| Piece | Path |
|-------|------|
| Priority values | `Cornerstone/Cornerstone/Presentation/DispatcherPriority.cs` |
| Drain loop | `Cornerstone.Presentation/Threading/Dispatcher.Queue.cs` |
| 50 ms assignment | `Cornerstone.Presentation/Threading/Dispatcher.cs` (`ReplaceImplementation`) |
| Starvation option | `Cornerstone.Presentation/Threading/DispatcherOptions.cs` |
| Schedule, clock, 153 guard | `Cornerstone.Presentation/Media/MediaContext.cs` |
| Commit throttle | `Cornerstone.Presentation/Media/MediaContext.Compositor.cs` |
| Layout passes | `Cornerstone.Presentation/Layout/LayoutManager.cs` |
| Invalidate no-op | `Cornerstone.Presentation/Layout/Layoutable.cs` |
| Enqueue cap | `Cornerstone.Presentation/Layout/LayoutQueue.cs` |
| Infinite scroll measure | `Cornerstone.Presentation/Controls/ScrollContentPresenter.cs` |
| Default panels | `Controls/ItemsControl.cs`, `Controls/ListBox.cs`, `Controls/ComboBox.cs` |
| Row virtualization | `Controls/VirtualizingStackPanel.cs`, `Controls/TreeDataGridPresenterBase.cs` |
| Text rebuild | `Controls/TextBlock.cs`, `Controls/TextPresenter.cs`, `Media/TextFormatting/TextLayout.cs`, `Media/TextFormatting/TextRunCache.cs` |
| Queue graph | `Controls/ChannelControl.cs` |
| Dirty visual + UI render | `Rendering/Composition/CompositingRenderer.cs` |
| UI commit | `Rendering/Composition/Compositor.cs` |
| Dirty-rect choice | `Rendering/Composition/Server/ServerCompositionTarget.cs`, `CompositionOptions.cs` |
| Single-rect union | `Rendering/Composition/Server/DirtyRects/SingleDirtyRectTracker.cs` |
| Walk skips single-rect tracker | `Rendering/Composition/Server/ServerCompositionVisual/ServerCompositionVisual.Render.cs` |
| Browser dispatcher | `Platforms/Browser/BrowserSingleThreadedDispatcherImpl.cs` |
| Browser post | `Platforms/Browser/webapp/modules/cornerstone/singleThreadedDispatcher.ts` |
| Browser clock hack | `Theme/Controls/BrowserThreads.cxaml` |
| Compositor pause | `Rendering/Composition/Server/ServerCompositor.cs` (`CommitGraceTicks`) |
| Airspace stamp | `Rendering/Composition/Server/ServerCompositionTarget.cs` (`StampAirspaceTreeOrder`) |
| Browser frame loop | `Platforms/Browser/Rendering/BrowserRenderTimer.cs`, `Platforms/Browser/webapp/modules/cornerstone/timer.ts` |
