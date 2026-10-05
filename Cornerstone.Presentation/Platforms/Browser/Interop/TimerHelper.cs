using System;
using System.Runtime.InteropServices.JavaScript;

namespace Cornerstone.Presentation.Browser.Interop;

internal static partial class TimerHelper
{
    [JSImport("TimerHelper.runAnimationFrames", CornerstoneModule.MainModuleName)]
    public static partial void RunAnimationFrames();

    public static Action<double>? AnimationFrame;
    [JSExport]
    public static void JsExportOnAnimationFrame(double d)
    {
        AnimationFrame?.Invoke(d);
    }
}
