using System;

using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.OpenGL;
using Cornerstone.Presentation.OpenGL.Surfaces;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Surfaces;

namespace Cornerstone.Presentation.Platforms.Windows.DirectX;

public interface IDirect3D11TexturePlatformSurface : IPlatformRenderSurface
{
    public IDirect3D11TextureRenderTarget CreateRenderTarget(IPlatformGraphicsContext graphicsContext, IntPtr d3dDevice);
}

[PrivateApi]
public interface IDirect3D11TexturePlatformSurface2 : IPlatformRenderSurface
{
    IDirect3D11TextureRenderTarget2 CreateRenderTarget(IPlatformGraphicsContext graphicsContext, IntPtr d3dDevice);
}


public interface IDirect3D11TextureRenderTarget : IPlatformRenderSurfaceRenderTarget, IDisposable
{
    IDirect3D11TextureRenderTargetRenderSession BeginDraw();
}

[PrivateApi]
public interface IDirect3D11TextureRenderTarget2 : IPlatformRenderSurfaceRenderTarget, IDisposable
{
    IDirect3D11TextureRenderTargetRenderSession BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo);
}

public interface IDirect3D11TextureRenderTargetRenderSession : IDisposable
{
    public IntPtr D3D11Texture2D { get; }
    public PixelSize Size { get; }
    public PixelPoint Offset { get; }
    public double Scaling { get; }
}
