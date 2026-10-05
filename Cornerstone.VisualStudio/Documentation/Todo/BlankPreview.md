# Blank preview with no error

**Status:** Open. Reproduced. The host loads the XAML and replies. No frame is painted.  
**Date:** 2026-09-27  
**Area:** Cornerstone.VisualStudio designer / previewer

## Resume here

The design surface stays on the empty preview chrome: the blue design grid and a black 100×100 frame. There is no error banner. The preview process stays running.

The last Information log (pid 66484) was:

```
Preview XAML sent at "7:23:52 PM". Waiting for the host.
Preview host: loading XAML
Preview host: XAML loaded
Preview host: result sent
Preview host accepted XAML at "7:23:53 PM". No frame painted yet.
```

That rules out a stuck `LoadDesignerWindow` and a dropped `UpdateXamlResult`. The host finished and reported no error. The designer never painted a bitmap, so the 100×100 placeholder stayed up.

Rebuild the extension and the designer host, preview again, and read the next host line:

| Host line | Meaning |
|-----------|---------|
| `Preview host: frame sent {width}x{height}` | A frame left the host. The designer should then log `Preview frame painted`. |
| `Preview host: frame skipped (pixel format is not set)` | The host has no pixel format, so it will not draw. |
| `Preview host: frame skipped (framebuffer was not rendered)` | A draw was requested and the framebuffer stayed empty. |
| `Preview host: frame skipped (waiting for ack of frame N)` | One frame was counted as sent and the host is waiting for the designer to acknowledge it. |

`PreviewerWindowImpl.Show` now posts `RenderAndSendFrameIfNeeded` after show returns. The resize render during `Show` runs before `StartRendering`, so that earlier pass often paints nothing.

## How to read the process row

**Showing** is set only after a frame is painted. These activity strings are also Information logs. They do not include pixel buffers.

| Activity | Meaning |
|----------|---------|
| `Sent {time}` | XAML left Visual Studio. The host has not answered, and nothing has been painted. |
| `Host accepted {time}` | The host took the XAML and reported no error. No frame has been painted. |
| `Frame {width}x{height} {time}` | A frame was painted at that size. If the surface is still the empty box, the designer received pixels and did not show them. |
| `Ignored frame {width}x{height} {time}` | A frame arrived and was discarded because it was 1×1 or smaller. |

A normal refresh logs the send, then a painted frame. This bug logs the send, `XAML loaded`, `result sent`, and `Host accepted`, with no painted frame.

## Already fixed while chasing this

These matched an earlier log that stopped at `Preview XAML sent` (pid 63528, three sends, no reply):

- The Cornerstone session never set `_connection`. `ProcessNonFrameMessageAsync` treated that as “no connection” and dropped `UpdateXamlResult`.
- The host writes the XAML result on the same connection as a frame. A second send used to throw “Previous send operation was not finished”, so the result could be lost.

## Where it lives

- `Cornerstone.VisualStudio/Services/PreviewerProcess.cs` — status and the Information lines.
- `Cornerstone.Presentation.Remote.Wpf/RemoteSession.cs` — `FrameIgnored` for an empty frame after a real bitmap.
- `Cornerstone.Presentation.Remote.Protocol/BsonStreamTransport.cs` — sends queue instead of failing the second write.
- `Cornerstone.Designer.HostApp/Remote/RemoteDesignerEntryPoint.cs` — `loading` / `loaded` / `result sent`.
- `Cornerstone.Designer.HostApp/Remote/PreviewerWindowImpl.cs` — `Show` posts another render.
- `Cornerstone.Presentation/Controls/Remote/Server/RemoteServerTopLevelImpl.cs` — `frame sent` / `frame skipped`.
- `Cornerstone.VisualStudio/Views/CornerstonePreviewer.xaml` — the black border and the default 100×100 image.
