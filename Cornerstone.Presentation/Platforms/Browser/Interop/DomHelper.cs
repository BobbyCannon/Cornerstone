using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Browser.Interop;

internal static partial class DomHelper
{
    [JSImport("CornerstoneDOM.getGlobalThis", CornerstoneModule.MainModuleName)]
    internal static partial JSObject GetGlobalThis();

    [JSImport("CornerstoneDOM.getFirstElementById", CornerstoneModule.MainModuleName)]
    internal static partial JSObject? GetElementById(string id, JSObject parent);

    [JSImport("CornerstoneDOM.getFirstElementByClassName", CornerstoneModule.MainModuleName)]
    internal static partial JSObject? GetElementsByClassName(string className, JSObject parent);

    [JSImport("CornerstoneDOM.createCornerstoneHost", CornerstoneModule.MainModuleName)]
    public static partial JSObject CreateCornerstoneHost(JSObject element);

    [JSImport("CornerstoneDOM.isFullscreen", CornerstoneModule.MainModuleName)]
    public static partial bool IsFullscreen(JSObject globalThis);

    [JSImport("CornerstoneDOM.setFullscreen", CornerstoneModule.MainModuleName)]
    public static partial Task SetFullscreen(JSObject globalThis, bool isFullscreen);

    [JSImport("CornerstoneDOM.getSafeAreaPadding", CornerstoneModule.MainModuleName)]
    public static partial double[] GetSafeAreaPadding(JSObject globalThis);

    [JSImport("CornerstoneDOM.getDarkMode", CornerstoneModule.MainModuleName)]
    public static partial int[] GetDarkMode(JSObject globalThis);

    [JSImport("CornerstoneDOM.getNavigatorLanguage", CornerstoneModule.MainModuleName)]
    public static partial string? GetNavigatorLanguage(JSObject globalThis);

    [JSImport("CornerstoneDOM.addClass", CornerstoneModule.MainModuleName)]
    public static partial void AddCssClass(JSObject element, string className);

    [JSImport("CornerstoneDOM.initGlobalDomEvents", CornerstoneModule.MainModuleName)]
    public static partial void InitGlobalDomEvents(JSObject globalThis);

    [JSExport]
    public static Task DarkModeChanged(bool isDarkMode, bool isHighContrast)
    {
        using var _ = JsCallbackHelper.EnsureDispatcherContext();
        (PresentationLocator.Current.GetService<IPlatformSettings>() as BrowserPlatformSettings)?.OnColorValuesChanged(isDarkMode, isHighContrast);
        return Task.CompletedTask;
    }

    [JSExport]
    public static Task DocumentVisibilityChanged(string visibilityState)
    {
        using var _ = JsCallbackHelper.EnsureDispatcherContext();
        (PresentationLocator.Current.GetService<IActivatableLifetime>() as BrowserActivatableLifetime)?.OnVisibilityStateChanged(visibilityState);
        return Task.CompletedTask;
    }

    [JSExport]
    public static Task LanguageChanged(string language)
    {
        using var _ = JsCallbackHelper.EnsureDispatcherContext();
        (PresentationLocator.Current.GetService<IPlatformSettings>() as BrowserPlatformSettings)?.OnPreferredLanguageChanged(language);
        return Task.CompletedTask;
    }

    [JSExport]
    public static Task ScreensChanged()
    {
        using var _ = JsCallbackHelper.EnsureDispatcherContext();
        (PresentationLocator.Current.GetService<IScreenImpl>() as BrowserScreens)?.OnChanged();
        return Task.CompletedTask;
    }
}
