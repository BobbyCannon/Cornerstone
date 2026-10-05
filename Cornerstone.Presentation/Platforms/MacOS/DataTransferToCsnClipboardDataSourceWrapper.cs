using System;
using System.Linq;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platforms.MacOS.Interop;

namespace Cornerstone.Presentation.Platforms.MacOS;

/// <summary>
/// Wraps a <see cref="IDataTransfer"/> into a <see cref="ICsnClipboardDataSource"/>.
/// This class is called by native code.
/// </summary>
/// <param name="dataTransfer">The data transfer object to wrap.</param>
internal sealed class DataTransferToCsnClipboardDataSourceWrapper(IDataTransfer dataTransfer)
    : NativeOwned, ICsnClipboardDataSource
{
    private IDataTransfer? _dataTransfer = dataTransfer;
    private DataTransferItemToCsnClipboardDataItemWrapper[]? _items;

    private IDataTransfer DataTransfer
        => _dataTransfer ?? throw new ObjectDisposedException(nameof(DataTransferToCsnClipboardDataSourceWrapper));

    private DataTransferItemToCsnClipboardDataItemWrapper[] Items
    {
        get
        {
            if (_items is null)
            {
                _items = GetItemsCore();

                if (_items.Length == 0)
                    Destroyed();
            }

            return _items;

            DataTransferItemToCsnClipboardDataItemWrapper[] GetItemsCore()
                => DataTransfer.Items
                    .Select(static item => new DataTransferItemToCsnClipboardDataItemWrapper(item))
                    .ToArray();
        }
    }

    public int ItemCount
        => Items.Length;

    public ICsnClipboardDataItem GetItem(int index)
        => Items[index];

    protected override void Destroyed()
    {
        _dataTransfer?.Dispose();
        _dataTransfer = null;
    }
}
