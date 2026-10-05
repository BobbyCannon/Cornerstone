using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Controls.Platform
{
    [Unstable]
    public interface INativeControlHostImpl
    {
        INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent);
        INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create);
        INativeControlHostControlTopLevelAttachment CreateNewAttachment(IPlatformHandle handle);
        bool IsCompatibleWith(IPlatformHandle handle);
    }

    [Unstable]
    public interface INativeControlHostDestroyableControlHandle : IPlatformHandle
    {
        void Destroy();
    }

    [Unstable]
    public interface INativeControlHostControlTopLevelAttachment : IDisposable
    {
        INativeControlHostImpl? AttachedTo { get; set; }

        bool IsCompatibleWith(INativeControlHostImpl host);
        void HideWithSize(Size size);
        void ShowInBounds(Rect rect);
    }
}
