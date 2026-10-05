using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Metal;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.Utilities;

namespace Cornerstone.Presentation.Platforms.MacOS;

class MetalPlatformGraphics : IPlatformGraphics
{
    private readonly ICornerstoneNativeFactory _factory;
    private readonly ICsnMetalDisplay _display;

    public MetalPlatformGraphics(ICornerstoneNativeFactory factory)
    {
        _factory = factory;
        _display = factory.ObtainMetalDisplay();
    }
    public bool UsesSharedContext => false;
    public IPlatformGraphicsContext CreateContext() => new MetalDevice(_factory, _display.CreateDevice());

    public IPlatformGraphicsContext GetSharedContext() => throw new NotSupportedException();
}

class MetalDevice : IMetalDevice
{
    private readonly DisposableLock _syncRoot = new();
    private readonly GpuHandleWrapFeature _handleWrapFeature;
    private readonly MetalExternalObjectsFeature _externalObjectsFeature;
    private ICsnMetalDevice? _native;

    public MetalDevice(ICornerstoneNativeFactory factory, ICsnMetalDevice native)
    {
        _native = native;
        _handleWrapFeature = new GpuHandleWrapFeature(factory);
        _externalObjectsFeature = new MetalExternalObjectsFeature(native);
    }

    public ICsnMetalDevice Native
    {
        get
        {
            ObjectDisposedException.ThrowIf(_native is null, this);
            return _native;
        }
    }

    public void Dispose()
    {
        _native?.Dispose();
        _native = null;
    }

    public object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(IExternalObjectsHandleWrapRenderInterfaceContextFeature))
            return _handleWrapFeature;
        if (featureType == typeof(IMetalExternalObjectsFeature))
            return _externalObjectsFeature;
        return null;
    }

    public bool IsLost => false;

    public IDisposable EnsureCurrent() => _syncRoot.Lock();

    public IntPtr Device => Native.Device;
    public IntPtr CommandQueue => Native.Queue;
}

class MetalPlatformSurface : IMetalPlatformSurface
{
    private readonly ICsnTopLevel _topLevel;

    public MetalPlatformSurface(ICsnTopLevel topLevel)
    {
        _topLevel = topLevel;
    }
    public IMetalPlatformSurfaceRenderTarget CreateMetalRenderTarget(IMetalDevice device)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            throw new RenderTargetNotReadyException();
        
        var dev = (MetalDevice)device;
        var target = _topLevel.CreateMetalRenderTarget(dev.Native);
        return new MetalRenderTarget(target);
    }
}

internal class MetalExternalObjectsFeature : IMetalExternalObjectsFeature
{
    private readonly ICsnMetalDevice _device;

    public unsafe MetalExternalObjectsFeature(ICsnMetalDevice device)
    {
        _device = device;
        ulong registryId;
        if (_device.GetIOKitRegistryId(&registryId) != 0)
        {
            var bytes = BitConverter.GetBytes(registryId);
            bytes.AsSpan().Reverse();
            DeviceLuid = bytes;
        }
    }

    public IReadOnlyList<string> SupportedImageHandleTypes { get; } =
        [KnownPlatformGraphicsExternalImageHandleTypes.IOSurfaceRef];

    public IReadOnlyList<string> SupportedSemaphoreTypes { get; } =
        [KnownPlatformGraphicsExternalSemaphoreHandleTypes.MetalSharedEvent];
    
    public byte[]? DeviceLuid { get; }

    public CompositionGpuImportedImageSynchronizationCapabilities
        GetSynchronizationCapabilities(string imageHandleType) =>
        CompositionGpuImportedImageSynchronizationCapabilities.TimelineSemaphores;

    public IMetalExternalTexture ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties)
    {
        var format = properties.Format switch
        {
            PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm => CsnPixelFormat.kCsnRgba8888,
            PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm => CsnPixelFormat.kCsnBgra8888,
            _ => throw new NotSupportedException("Pixel format is not supported")
        };
        
        if (handle.HandleDescriptor != KnownPlatformGraphicsExternalImageHandleTypes.IOSurfaceRef)
            throw new NotSupportedException();

        return new ImportedTexture(_device.ImportIOSurface(handle.Handle, format));
    }

    public IMetalSharedEvent ImportSharedEvent(IPlatformHandle handle)
    {
        if (handle.HandleDescriptor != KnownPlatformGraphicsExternalSemaphoreHandleTypes.MetalSharedEvent)
            throw new NotSupportedException();
        return new SharedEvent(_device.ImportSharedEvent(handle.Handle));
    }

    class ImportedTexture(ICsnMetalTexture texture) : IMetalExternalTexture
    {
        public void Dispose() => texture.Dispose();

        public int Width => texture.Width;

        public int Height => texture.Height;

        public int Samples => texture.SampleCount;

        public IntPtr Handle => texture.NativeHandle;
    }
    
    class SharedEvent(ICsnMTLSharedEvent inner) : IMetalSharedEvent
    {
        public ICsnMTLSharedEvent Native => inner;
        public void Dispose()
        {
            inner.Dispose();
        }

        public IntPtr Handle => inner.NativeHandle;
    }

    public void SubmitWait(IMetalSharedEvent @event, ulong waitForValue) =>
        _device.SubmitWait(((SharedEvent)@event).Native, waitForValue);

    public void SubmitSignal(IMetalSharedEvent @event, ulong signalValue) =>
        _device.SubmitSignal(((SharedEvent)@event).Native, signalValue);
}

internal class MetalRenderTarget : IMetalPlatformSurfaceRenderTarget
{
    private ICsnMetalRenderTarget? _native;

    public MetalRenderTarget(ICsnMetalRenderTarget native)
    {
        _native = native;
    }

    private ICsnMetalRenderTarget Native
    {
        get
        {
            ObjectDisposedException.ThrowIf(_native is null, this);
            return _native;
        }
    }

    public void Dispose()
    {
        _native?.Dispose();
        _native = null;
    }

    public IMetalPlatformSurfaceRenderingSession BeginRendering()
    {
        var session = Native.BeginDrawing();
        return new MetalDrawingSession(session);
    }
}

internal class MetalDrawingSession : IMetalPlatformSurfaceRenderingSession
{
    private ICsnMetalRenderingSession? _session;

    public MetalDrawingSession(ICsnMetalRenderingSession session)
    {
        _session = session;
    }

    public ICsnMetalRenderingSession Session
    {
        get
        {
            ObjectDisposedException.ThrowIf(_session is null, this);
            return _session;
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
        _session = null;
    }

    public IntPtr Texture => Session.Texture;

    public PixelSize Size
    {
        get
        {
            var size = Session.PixelSize;
            return new(size.Width, size.Height);
        }
    }

    public double Scaling => Session.Scaling;

    public bool IsYFlipped => false;
}
