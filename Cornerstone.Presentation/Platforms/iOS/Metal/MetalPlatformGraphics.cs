using System;
using System.Runtime.Versioning;
using Cornerstone.Presentation.Platform;
using Metal;

namespace Cornerstone.Presentation.iOS.Metal;

[SupportedOSPlatform("ios")]
[SupportedOSPlatform("macos")]
[SupportedOSPlatform("maccatalyst")]
[SupportedOSPlatform("tvos")]
internal class MetalPlatformGraphics : IPlatformGraphics
{
    private readonly IMTLDevice _defaultDevice;

    private MetalPlatformGraphics(IMTLDevice defaultDevice)
    {
        _defaultDevice = defaultDevice;
    }
    
    public bool UsesSharedContext => false;
    public IPlatformGraphicsContext CreateContext() => new MetalDevice(_defaultDevice);

    public IPlatformGraphicsContext GetSharedContext() => throw new NotSupportedException();

    public static MetalPlatformGraphics? TryCreate()
    {
        var device = MTLDevice.SystemDefault;
        if (device is null)
        {
            // Can be null on unsupported OS versions.
            return null;
        }

        return new MetalPlatformGraphics(device);
    }
}
