using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Cornerstone.Presentation.Threading;

namespace Cornerstone.Presentation.Browser.Interop;

internal record GLInfo(int ContextId, uint FboId, int Stencils, int Samples, int Depth);

internal static partial class CanvasHelper
{
    [JSExport]
    public static Task OnSizeChanged(int topLevelId, double width, double height, double dpr)
    {
        if (BrowserWindowingPlatform.IsThreadingEnabled)
        {
            return Dispatcher.UIThread.InvokeAsync(() => BrowserTopLevelImpl
                    .TryGetTopLevel(topLevelId)?.Surface?.OnSizeChanged(width, height, dpr))
                .GetTask();
        }
        else
        {
            using var _ = JsCallbackHelper.EnsureDispatcherContext();
            BrowserTopLevelImpl
                .TryGetTopLevel(topLevelId)?.Surface?.OnSizeChanged(width, height, dpr);
            return Task.CompletedTask;
        }
    }

    [JSImport("CanvasSurface.create", CornerstoneModule.MainModuleName)]
    public static partial JSObject CreateRenderTargetSurface(JSObject canvasSurface, int[] modes, int topLevelId, int threadId);

    [JSImport("CanvasSurface.destroy", CornerstoneModule.MainModuleName)]
    public static partial void Destroy(JSObject canvasSurface);
}
