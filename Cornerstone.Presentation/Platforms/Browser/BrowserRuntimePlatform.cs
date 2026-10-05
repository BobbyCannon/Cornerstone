using System;
using System.Reflection;
using Cornerstone.Presentation.Browser.Interop;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Browser;

internal static class BrowserRuntimePlatformServices
{
    public static AppBuilder UseBrowserRuntimePlatformSubsystem(this AppBuilder builder)
    {
        builder.UseRuntimePlatformSubsystem(() => Register(builder.ApplicationType?.Assembly), nameof(BrowserRuntimePlatform));
        return builder;
    }
    
    public static void Register(Assembly? assembly = null)
    {
        AssetLoader.RegisterResUriParsers();
        PresentationLocator.CurrentMutable
            .Bind<IRuntimePlatform>().ToSingleton<BrowserRuntimePlatform>()
            .Bind<IAssetLoader>().ToConstant(new StandardAssetLoader(assembly));
    }
}

internal class BrowserRuntimePlatform : StandardRuntimePlatform
{
    private static readonly Lazy<RuntimePlatformInfo> Info = new(() =>
    {
        var isMobile = CornerstoneModule.IsMobile();
        var isTv = CornerstoneModule.IsTv();
        var result = new RuntimePlatformInfo
        {
            IsMobile = isMobile && !isTv,
            IsDesktop = !isMobile && !isTv,
            IsTV = isTv
        };
        
        return result;
    });

    public override RuntimePlatformInfo GetRuntimeInfo() => Info.Value;
}
