using System;
using System.ComponentModel;
using System.Threading;
using Cornerstone.Presentation.Platforms.Windows.Interop;
using MicroCom.Runtime;

namespace Cornerstone.Presentation.Platforms.Windows.DComposition;

internal class DirectCompositionShared : IDisposable
{
    public object SyncRoot { get; } = new();

    public DirectCompositionShared(IDCompositionDesktopDevice device)
    {
        Device = device.CloneReference();
    }

    public IDCompositionDesktopDevice Device { get; }

    public void Dispose()
    {
        Device.Dispose();
    }
}
