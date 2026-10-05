# Native layering

Native views (WebView, camera, and other `NativeControlHost` islands) sit **under** Cornerstone’s Skia plane. Where a native control should show, that plane is transparent and hit-test skips unless a later Cornerstone visual hit-tests at that pixel. That visual can be fully transparent and still take the event. Overlay buttons, menus, and adorners stay on top of live native content.

This is the supported composition model, not a workaround. `WebView.IsPaused` remains for menus, designers, X11, and hosts that turn native-behind off.

---

## Two planes

Every Cornerstone visual composites **above** every native island. Native never covers managed UI. Overlapping natives are ordered only **inside holes**. There is no Native → Skia → Native sandwich.

```
  [ Skia plane: opaque chrome | hole (clear, click-through unless chrome) ]
                              |
                              v
  [ Native plane: WebView / platform view, live ]
```

A hole is a portal at the host’s tree order, not “clip the window background and hope.” Ancestors and earlier siblings do not fill the hole. Position and opacity do not change that. The `NativeControlHost` itself has no Skia content. Later siblings draw over the hole as usual.

---

## Holes

Each attached, effectively visible `NativeControlHost` contributes a hole:

- Geometry is a clipped rect shared by the hole and `ShowInBounds` (ancestor `Clip` / `ClipToBounds` / window client). Fully scrolled out: empty hole and hide the native surface.
- Rounded ancestor clips shrink onto that rect for Skia and hit-test. The native surface itself may stay rectangular; opaque Skia pixels hide native outside the path.
- The compositor dirties old and new hole rects so the plane re-clears.

Input:

- Point in a hole that no Cornerstone visual hit-tests → native (click, wheel, type, IME).
- Point on Cornerstone chrome (button over the web) → Cornerstone, even inside the host layout rect.
- A fully transparent visual takes the event when it has hit geometry and `IsHitTestVisible` is set. `Background="Transparent"` and `Opacity="0"` both count. A visual with no brush and no other hit geometry does not.

Do not treat the entire host bounds as pass-through. That would make overlay chrome unclickable.

---

## Windows

One top-level **frame** window plus one **layered composition** window covering the client:

| Layer | Role |
|-------|------|
| Frame HWND | Redirection bitmap. Native children (WebView2, and so on) are real child windows and paint live. |
| Composition HWND | `WS_CHILD` + `WS_EX_LAYERED`, covering the client. Skia presents here with premultiplied BGRA via `UpdateLayeredWindow` on the UI thread, always with a size. |

Pixels with alpha 0 in the composition window click through to the frame, so the native child gets real OS hits. Where a hit-testable Cornerstone visual covers the hole and painted nothing, the presented bitmap keeps an alpha of 1 so the click stays in Cornerstone and the native control still shows through. Higher alpha stays as painted. Present size only from the UI thread; doing it from the compositor thread deadlocks.

GPU / DirectComposition on the composition window does not per-pixel click-through, so it is not used there. Putting the native HWND into the DComp tree, or raising DComp above child HWNDs on a single window, does not show **live** native through a clear rect (stale snapshot, or native on top and frozen).

Premultiplied alpha applies while native-behind is on and that window has a native host attached (or the window is already transparent). Opaque windows with no host stay the cheaper ignore-alpha path. Mica/Acrylic fill the remainder around holes, not the holes themselves.

---

## Other platforms

| Platform | Native plane | Skia plane |
|----------|--------------|------------|
| Android | Native `View` at index 0 | `TextureView`, `Opaque = false` |
| iOS | Native islands as **siblings below** `CornerstoneView` (not subviews — those sit on the Metal layer) | Metal/EAGL, `Opaque = false` |
| macOS | Native holder sibling below `CsnView` | Metal, `isOpaque = false`, hole hit-test |
| Wayland | `wl_subsurface` below the toplevel Skia surface (`PlaceBelow`) | Parent surface; holes are Skia-clear. Parent keeps a full input region so overlay chrome in a hole still hits Skia; the Linux WebView adapter injects pointer/wheel/key into WebKit. |
| X11 | Child window on top | No native-behind. Use pause. |
| Browser | DOM node | z-index / CSS; smallest delta |

Linux WebView is system WebKitGTK. X11 reparents the widget XID. Wayland cannot share Cornerstone’s `wl_display`, so GTK renders in a hidden X11 window and frames are copied into the subsurface.

---

## Pause

`PausableNativeHost` / `WebView.IsPaused` snapshots the native surface and hides it so Skia can own the rect. Use it when native-behind is off, on X11, or when you explicitly want a frozen page (overlay menus on child-on-top, designer, Sample). Do not use `IsVisible = false` on the whole WebView to clear airspace.

---

## Option

`NativeBehindComposition` on `Win32PlatformOptions`, `AndroidPlatformOptions`, `iOSPlatformOptions`, `MacOSPlatformOptions`, and `WaylandPlatformOptions` defaults **on**. Set it **false** to restore child-on-top (native above Skia). The flag is process-lifetime at windowing init, not per control.

X11 has no native-behind path.

---

## Limits

- The native control’s own pixels are not a transparency mask. The hole is a clear rect in the Skia plane. A hit-testable Cornerstone visual over that hole still receives pointer events when its paint is fully transparent.
- Popups are separate top-level windows and sit above everything; they need holes only if a popup hosts a native control.
- Android `SurfaceView` as the Skia plane does not show native through holes. TextureView is the Android path.
- WebKitGTK `GtkOffscreenWindow` is not used (WebKit 2.48 crash). Wayland blit uses a hidden real `GtkWindow`.
- Do not full-rect-clear the host after draw (kills overlay chrome). Do not leave portal hole-exclusion clipped onto descendants (`PostSubgraph`); pop it after `RenderCore`.
