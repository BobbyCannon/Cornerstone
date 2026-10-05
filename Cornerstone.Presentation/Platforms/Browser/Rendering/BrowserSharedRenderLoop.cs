using System;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Rendering.Composition;

namespace Cornerstone.Presentation.Browser.Rendering;

internal static class BrowserSharedRenderLoop
{
    private static BrowserRenderTimer? s_browserUiRenderTimer;
    public static BrowserRenderTimer RenderTimer => s_browserUiRenderTimer ??= new BrowserRenderTimer(false);
    public static Lazy<IRenderLoop> RenderLoop = new(() => Cornerstone.Presentation.Rendering.RenderLoop.FromTimer(RenderTimer), true);
}
