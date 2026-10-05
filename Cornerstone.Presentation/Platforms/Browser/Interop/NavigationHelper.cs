using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Browser.Interop;

internal static partial class NavigationHelper
{
    [JSImport("NavigationHelper.addBackHandler", CornerstoneModule.MainModuleName)]
    public static partial void AddBackHandler([JSMarshalAs<JSType.Function<JSType.Boolean>>] Func<bool> backHandlerCallback);

    public static Task<bool> OnBackRequested()
    {
        var handled = (PresentationLocator.Current.GetService<ISystemNavigationManagerImpl>() as BrowserSystemNavigationManagerImpl)?
            .OnBackRequested() ?? false;
        return Task.FromResult(handled);
    }

    [JSImport("NavigationHelper.openUri", CornerstoneModule.MainModuleName)]
    public static partial bool WindowOpen(string uri, string target);
}
