using System;
using Cornerstone.Presentation.Platform.Surfaces;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Vulkan;

[NotClientImplementable]
public interface IVulkanRenderTarget : IDisposable, IPlatformRenderSurfaceRenderTarget
{
    IVulkanRenderSession BeginDraw();
}

[NotClientImplementable]
public interface IVulkanRenderSession : IDisposable
{
    double Scaling { get; }
    PixelSize Size { get; }
    public bool IsYFlipped { get; }
    VulkanImageInfo ImageInfo { get; }
    bool IsRgba { get; }
}