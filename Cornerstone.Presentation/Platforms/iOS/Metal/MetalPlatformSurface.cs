using Cornerstone.Presentation.Metal;
using CoreAnimation;

namespace Cornerstone.Presentation.iOS.Metal;

internal class MetalPlatformSurface : IMetalPlatformSurface
{
    private readonly CAMetalLayer _layer;
    private readonly CornerstoneView _cornerstoneView;

    public MetalPlatformSurface(CAMetalLayer layer, CornerstoneView cornerstoneView)
    {
        _layer = layer;
        _cornerstoneView = cornerstoneView;
    }
    public IMetalPlatformSurfaceRenderTarget CreateMetalRenderTarget(IMetalDevice device)
    {
        var dev = (MetalDevice)device;
        _layer.Device = dev.Device;

        var target = new MetalRenderTarget(_layer, dev);
        _cornerstoneView.SetRenderTarget(target);
        return target;
    }
}
