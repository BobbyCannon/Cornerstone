using System;
using System.Runtime.InteropServices.JavaScript;

namespace Cornerstone.Presentation.Browser.Interop;

internal static partial class NativeControlHostHelper
{
    [JSImport("NativeControlHost.createDefaultChild", CornerstoneModule.MainModuleName)]
    internal static partial JSObject CreateDefaultChild(JSObject? parent);

    [JSImport("NativeControlHost.createAttachment", CornerstoneModule.MainModuleName)]
    internal static partial JSObject CreateAttachment();

    [JSImport("NativeControlHost.initializeWithChildHandle", CornerstoneModule.MainModuleName)]
    internal static partial void InitializeWithChildHandle(JSObject element, JSObject child);

    [JSImport("NativeControlHost.attachTo", CornerstoneModule.MainModuleName)]
    internal static partial void AttachTo(JSObject element, JSObject? host);

    [JSImport("NativeControlHost.showInBounds", CornerstoneModule.MainModuleName)]
    internal static partial void ShowInBounds(JSObject element, double x, double y, double width, double height);

    [JSImport("NativeControlHost.hideWithSize", CornerstoneModule.MainModuleName)]
    internal static partial void HideWithSize(JSObject element, double width, double height);

    [JSImport("NativeControlHost.releaseChild", CornerstoneModule.MainModuleName)]
    internal static partial void ReleaseChild(JSObject element);
}
