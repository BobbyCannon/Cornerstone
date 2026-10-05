using System;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Platforms.MacOS;

class GpuHandleWrapFeature : IExternalObjectsHandleWrapRenderInterfaceContextFeature
{
    private readonly ICsnNativeObjectsMemoryManagement _helper;

    public GpuHandleWrapFeature(ICornerstoneNativeFactory factory)
    {
        _helper = factory.CreateMemoryManagementHelper();
    }
    public IExternalObjectsWrappedGpuHandle? WrapImageHandleOnAnyThread(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties)
    {
        if (handle.HandleDescriptor == KnownPlatformGraphicsExternalImageHandleTypes.IOSurfaceRef)
        {
            _helper.RetainCFObject(handle.Handle);
            return new CFObjectWrapper(_helper, handle.Handle, handle.HandleDescriptor);
        }

        return null;
    }

    public IExternalObjectsWrappedGpuHandle? WrapSemaphoreHandleOnAnyThread(IPlatformHandle handle)
    {
        if (handle.HandleDescriptor == KnownPlatformGraphicsExternalSemaphoreHandleTypes.MetalSharedEvent)
        {
            _helper.RetainNSObject(handle.Handle);
            return new NSObjectWrapper(_helper, handle.Handle, handle.HandleDescriptor);
        }

        return null;
    }

    class NSObjectWrapper(ICsnNativeObjectsMemoryManagement helper, IntPtr handle, string descriptor) : IExternalObjectsWrappedGpuHandle
    {
        public void Dispose() => helper.ReleaseNSObject(handle);

        public IntPtr Handle => handle;
        public string HandleDescriptor => descriptor;
    }
    
    class CFObjectWrapper(ICsnNativeObjectsMemoryManagement helper, IntPtr handle, string descriptor) : IExternalObjectsWrappedGpuHandle
    {
        public void Dispose()
        {
            helper.ReleaseCFObject(handle);
        }

        public IntPtr Handle => handle;
        public string HandleDescriptor => descriptor;
    }
}
