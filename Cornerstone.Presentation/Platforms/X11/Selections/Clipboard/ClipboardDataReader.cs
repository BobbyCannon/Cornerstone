using System;
using System.Threading.Tasks;
using Cornerstone.Presentation.Input;
using static Cornerstone.Presentation.X11.XLib;

namespace Cornerstone.Presentation.X11.Selections.Clipboard;

/// <summary>
/// An object used to read values, converted to the correct format, from the X11 clipboard.
/// </summary>
internal sealed class ClipboardDataReader(
    X11Platform platform,
    IntPtr selection,
    IntPtr[] textFormatAtoms,
    DataFormat[] dataFormats,
    IntPtr owner)
    : SelectionDataReader<IAsyncDataTransferItem>(platform.Info.Atoms, textFormatAtoms, dataFormats)
{
    private IntPtr _owner = owner;

    private bool IsOwnerStillValid()
        => _owner != IntPtr.Zero && XGetSelectionOwner(platform.Display, selection) == _owner;

    public override Task<object?> TryGetAsync(DataFormat format)
    {
        if (!IsOwnerStillValid())
            return Task.FromResult<object?>(null);

        return base.TryGetAsync(format);
    }

    protected override IAsyncDataTransferItem CreateSingleItem(DataFormat[] nonFileFormats)
        => new ClipboardDataTransferItem(this, nonFileFormats);

    protected override SelectionReadSession CreateReadSession()
        => ClipboardReadSessionFactory.CreateSession(platform, selection);

    public override void Dispose()
        => _owner = IntPtr.Zero;
}
