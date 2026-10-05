using System;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform
{
    [Unstable]
    public interface IMacOSTopLevelPlatformHandle
    {
        IntPtr NSView { get; }
        IntPtr GetNSViewRetained();
        IntPtr NSWindow { get; }
        IntPtr GetNSWindowRetained();
    }
}
