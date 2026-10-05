using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Cornerstone.Presentation.Browser.Interop;

internal static partial class ScreenHelper
{
    [JSImport("ScreenHelper.subscribeOnChanged", CornerstoneModule.MainModuleName)]
    public static partial void SubscribeOnChanged(JSObject globalThis);
    
    [JSImport("ScreenHelper.checkPermissions", CornerstoneModule.MainModuleName)]
    public static partial void CheckPermissions(JSObject globalThis);

    [JSImport("ScreenHelper.getAllScreens", CornerstoneModule.MainModuleName)]
    public static partial JSObject[] GetAllScreens(JSObject globalThis);

    [JSImport("ScreenHelper.requestDetailedScreens", CornerstoneModule.MainModuleName)]
    [return: JSMarshalAs<JSType.Promise<JSType.Boolean>>]
    public static partial Task<bool> RequestDetailedScreens(JSObject globalThis);

    [JSImport("ScreenHelper.getDisplayName", CornerstoneModule.MainModuleName)]
    public static partial string GetDisplayName(JSObject screen);

    [JSImport("ScreenHelper.getScaling", CornerstoneModule.MainModuleName)]
    public static partial double GetScaling(JSObject screen);

    [JSImport("ScreenHelper.getBounds", CornerstoneModule.MainModuleName)]
    public static partial double[] GetBounds(JSObject screen);

    [JSImport("ScreenHelper.getWorkingArea", CornerstoneModule.MainModuleName)]
    public static partial double[] GetWorkingArea(JSObject screen);

    [JSImport("ScreenHelper.isCurrent", CornerstoneModule.MainModuleName)]
    public static partial bool IsCurrent(JSObject screen);

    [JSImport("ScreenHelper.isPrimary", CornerstoneModule.MainModuleName)]
    public static partial bool IsPrimary(JSObject screen);

    [JSImport("ScreenHelper.getCurrentOrientation", CornerstoneModule.MainModuleName)]
    public static partial int GetCurrentOrientation(JSObject screen);
}
