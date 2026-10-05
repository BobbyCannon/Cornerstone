using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Cornerstone.Presentation.Browser.Interop;

internal static partial class CornerstoneModule
{
    // Resolved by the document import map during ImportMain on this thread, for contexts that have no import map.
    private static string s_mainModuleUrl;
    private static string s_storageModuleUrl;

    private static readonly Lazy<Task> s_importMain = new(async () =>
    {
        var options = PresentationLocator.Current.GetService<BrowserPlatformOptions>() ?? new BrowserPlatformOptions();
        await JSHost.ImportAsync(MainModuleName, options.FrameworkAssetPathResolver!("cornerstone.js"));

        s_mainModuleUrl = GetModuleUrl();
        s_storageModuleUrl = ResolveModuleUrl("./storage.js");
    });

    private static readonly Lazy<Task> s_importStorage = new(() =>
    {
        var options = PresentationLocator.Current.GetService<BrowserPlatformOptions>() ?? new BrowserPlatformOptions();
        return JSHost.ImportAsync(StorageModuleName, s_storageModuleUrl ?? options.FrameworkAssetPathResolver!("storage.js"));
    });

    /// <summary>
    /// Imports the main module into a web worker, which needs its own copy of every module it calls into.
    /// </summary>
    public static Task ImportMainToWorkerContext()
    {
        if (s_mainModuleUrl is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ImportMain)} has to complete on the main thread before a worker can import the module.");
        }

        return JSHost.ImportAsync(MainModuleName, s_mainModuleUrl);
    }

    public const string MainModuleName = "cornerstone";
    public const string StorageModuleName = "storage";

    public const string AssetsBasePath = "_content/Cornerstone.Presentation";

    public static Task ImportMain() => s_importMain.Value;

    public static Task ImportStorage() => s_importStorage.Value;

    /// <remarks>
    /// serviceWorker.register resolves the path against the document, not the caller framework,
    /// so FrameworkAssetPathResolver does not apply. The worker also has to sit at the app root:
    /// it is scoped to its own directory, and the save picker polyfill looks it up with
    /// getRegistration(), which matches against the document URL.
    /// </remarks>
    public static string ResolveServiceWorkerPath() => "./cornerstone-sw.js";

    [JSImport("Caniuse.isMobile", CornerstoneModule.MainModuleName)]
    public static partial bool IsMobile();

    [JSImport("Caniuse.isTv", CornerstoneModule.MainModuleName)]
    public static partial bool IsTv();

    [JSImport("registerServiceWorker", CornerstoneModule.MainModuleName)]
    public static partial void RegisterServiceWorker(string path, string? scope);

    [JSImport("getModuleUrl", CornerstoneModule.MainModuleName)]
    private static partial string GetModuleUrl();

    [JSImport("resolveModuleUrl", CornerstoneModule.MainModuleName)]
    private static partial string ResolveModuleUrl(string name);
}
