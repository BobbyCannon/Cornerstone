using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering.Composition;

namespace Cornerstone.Presentation.Metal;

[PrivateApi]
public interface IMetalExternalObjectsFeature
{
    IReadOnlyList<string> SupportedImageHandleTypes { get; }
    IReadOnlyList<string> SupportedSemaphoreTypes { get; }
    byte[]? DeviceLuid { get; }
    CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType);
    IMetalExternalTexture ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties);
    IMetalSharedEvent ImportSharedEvent(IPlatformHandle handle);

    void SubmitWait(IMetalSharedEvent @event, ulong waitForValue);
    void SubmitSignal(IMetalSharedEvent @event, ulong signalValue);
}

[PrivateApi]
public interface IMetalExternalTexture : IDisposable
{
    int Width { get; }
    int Height { get; }
    int Samples { get; }
    IntPtr Handle { get; }
}

[PrivateApi]
public interface IMetalSharedEvent : IDisposable
{
    IntPtr Handle { get; }
}
