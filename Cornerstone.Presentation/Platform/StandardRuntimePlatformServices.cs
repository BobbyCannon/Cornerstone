using System.Reflection;

namespace Cornerstone.Presentation.Platform;

internal static class StandardRuntimePlatformServices
{
    public static void Register(Assembly? assembly = null)
    {
        AssetLoader.RegisterResUriParsers();
        PresentationLocator.CurrentMutable
            .Bind<IRuntimePlatform>().ToSingleton<StandardRuntimePlatform>()
            .Bind<IAssetLoader>().ToConstant(new StandardAssetLoader(assembly));
    }
}
