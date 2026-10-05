using System;
using Cornerstone.Presentation.Input;

namespace Cornerstone.Presentation.X11.Selections.DragDrop;

internal sealed class DragDropDataProvider : SelectionDataProvider
{
    public Action? Activity { get; set; }

    public DragDropDataProvider(X11Platform platform, IAsyncDataTransfer dataTransfer)
        : base(platform, platform.Info.Atoms.XdndSelection)
        => DataTransfer = dataTransfer;

    public new IntPtr GetOwner()
        => base.GetOwner();

    public new void SetOwner(IntPtr window)
        => base.SetOwner(window);

    protected override void OnActivity()
        => Activity?.Invoke();

    public override void Dispose()
    {
        Activity = null;

        DataTransfer?.Dispose();
        DataTransfer = null;
    }
}
