# Native layering (implementer)

Human model: [../Presentation/NativeLayering.md](../Presentation/NativeLayering.md).

**Handoff (2026-09-07):** Shipped, default on. Operator testing. Next is not a new hosting path — test, then consider deleting the child-on-top off switch. X11 stays pause.

## File map

| Area | Path |
|------|------|
| Shared flag | `Platform/NativeAirspace.cs` (`BehindComposition`, hole list) |
| Hole clip | `Platform/NativeAirspaceClip.cs` (`ClipVisualOutOfHole` is strict `<`) |
| Skia region clip | `Backends/Skia/DrawingContextImpl.cs` (`PushClip(region)` in device pixels) |
| Host control | `Controls/NativeControlHost.cs` (Src-clear hole, hole publish) |
| Portal walk | `Rendering/Composition/ServerCompositionVisual.Render.cs` (clip around `RenderCore` and cache blits; pop before children) |
| Pause | Theme `PausableNativeHost` |
| WebView | Theme `WebView` + platform adapters |
| Win32 overlay HWND | `Win32/Win32NativeAirspaceOverlay.cs` |
| Win32 present | `Win32/FramebufferManager.cs` (`PresentLayeredOnUiThread`) |
| Win32 hit alpha | `Platform/NativeAirspaceHitAlpha.cs` (alpha-1 stamp for transparent overlays) |
| Win32 window | `Win32/WindowImpl.cs` (flag on → redirection frame, overlay surfaces) |
| Android | `Android/Platform/AndroidNativeControlHostImpl.cs`, `TextureViewImpl` |
| iOS | `iOS/NativeControlHostImpl.cs`, `iOS/CornerstoneView.cs` |
| macOS | `Native/` + ObjC `Native/Cornerstone.Native` |
| Wayland host | `Wayland/WaylandNativeControlHost.cs`, `Wayland/Server/Persistent/WNativeControlSubsurface.cs` |
| Linux WebView | Theme `Platforms/Desktop/LinuxWebViewAdapter.cs`, `LinuxWebKit.cs` |

## Do not retry

These failed as the **primary** native-behind path. Do not revive them under a new name.

- Child HWND (or GDI) “peek-through” under a DComp/WinUI tree on the **same** top-level: Skia looks on top; the hole is a stale snapshot.
- Native HWND as a DComp visual (`CreateSurfaceFromHwnd`, cloaked popup): native on top of chrome and frozen.
- DirectComposition / GPU on the **layered composition** HWND: no per-pixel click-through.
- `SendMessage` mouse/wheel/focus into WebView2: Chromium ignores synthetic input.
- `UpdateLayeredWindow` / `SetWindowPos` size from the compositor thread: deadlock.
- First `UpdateLayeredWindow` with NULL size: layered window never shows Skia.
- Android `SurfaceView` z-order-on-top + transparent format: WebView black.
- iOS `HandleResize` `Frame (0,0)` or CSS zoom = device scale; `ScrollsToTop` on a window-sibling WKWebView.
- WebKitGTK `GtkOffscreenWindow` (2.48 `gdk_window_get_origin` / `isInMonitor` crash). Hidden real `GtkWindow` + cairo blit instead.
- `gdk_window_set_composited` on XWayland: `BadAccess` CompositeRedirectWindow.
- Portal clip left until `PostSubgraph` (descendants inherit hole-exclusion; overlay chrome vanishes).

X11 stays child-on-top + pause. No X11 native-behind.

## Hole punch

Tree order is preorder, stamped each frame on `ServerCompositionVisual.AirspaceTreeOrder`.

- Visuals before the host are clipped out of that hole. The host’s own order is not. `NativeControlHost.Render` draws transparent with `SKBlendMode.Src`, and that clear has to land in the hole. `ClipVisualOutOfHole` is `<`, not `<=`. `<=` clips the clear away, so an earlier sibling (the sample shield in `AppView.cxaml`) stays in the composition bitmap over live video.
- Later siblings draw over the hole. Pop the portal clip before children. A clip that lasts until `PostSubgraph` hides overlay chrome.
- Bitmap-cache blits take the same clip. `Cache.Draw` used to return before the clip and could bake earlier content into the hole.
- Regions are physical pixels. `SKCanvas.ClipRegion` runs them through the current matrix unless that matrix is identity, so a translated visual misses the hole. `DrawingContextImpl.PushClip(IPlatformRenderInterfaceRegion)` resets the matrix, clips, then puts the matrix back. Dirty-rect clips use the same method and the same pixel space.
- `UseOpacitySaveLayer` defaults off, so control opacity is paint alpha on the same canvas as the clear.

`NativeAirspaceHoleTests.EarlierSiblingDoesNotFillNativeHole` covers scale 1 and 1.5: the overlap stays clear, the earlier border still paints outside the hole, the later border still paints inside, and the overlap point hits `NativeControlHost`. Hit-testing and the Windows alpha stamp were not changed. The sample Media Player tab was not relaunched after this fix.

## Interactive transparent overlay

A hit-testable Cornerstone visual over a hole receives pointer events even when it paints alpha 0 (`Background="Transparent"`, or `Opacity="0"`). Uncovered hole pixels stay alpha 0 and go to native. A visual with no brush and no other hit geometry does not take the event.

Windows `UpdateLayeredWindow` never delivers `WM_NCHITTEST` for alpha 0. `NativeAirspaceHitAlpha` stamps alpha 1 on that coverage before present, and clears the black alpha-1 sentinel when coverage moves. Do not stamp uncovered hole pixels. Do not move the composition window to DirectComposition for this; that path has no per-pixel click-through.

Other platforms already route with `InputHitTest` (`IsInteractiveOverlay` / `IsHoleHit`).

## Flag

`NativeBehindComposition` defaults **on**. Process-lifetime. Windows is the expensive dual present (layered composition HWND vs DComp child-on-top). Other platforms are z-order / opaque / TextureView vs SurfaceView at init. Do not add a per-control flag.
