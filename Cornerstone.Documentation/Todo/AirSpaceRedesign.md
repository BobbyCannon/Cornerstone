# Native layering — completed

**Status:** Session ended. Shipped. Default on.  
**Date:** 2026-09-07  
**Branch:** `macos-wayland`

Pickup: [Presentation/NativeLayering.md](../Presentation/NativeLayering.md) (how it works). Implementer map: [Agent/NativeLayering.md](../Agent/NativeLayering.md). WV001: [KnownIssues.md](../KnownIssues.md).

Do not treat this file as the design. Git history has the working log.

### Session ended (2026-09-07)

Native-behind composition is the supported model. `NativeBehindComposition` defaults **on** (Win32, Android, iOS, macOS, Wayland). Set false for child-on-top. X11 has no native-behind path. The freeze-frame pause API has been removed.

Operator: product testing with default on. After that, consider dropping the off path.

**Do not:** DComp peek-through on the frame, GPU on the layered composition window, SurfaceView as the Skia plane, GtkOffscreenWindow, X11 native-behind, restore Sample env-gate.

Restart: not invert work. Test default-on, or drop `NativeBehindComposition == false` when testing is done.
