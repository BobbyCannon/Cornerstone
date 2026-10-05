using System;
using System.IO;
using Android.App;
using Android.Content;
using Cornerstone.Presentation.Android.Platform.Storage;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform.Storage;

namespace Cornerstone.Presentation.Android.Platform;

/// <summary>
/// Wraps a <see cref="ClipData.Item"/> into a <see cref="IDataTransferItem"/>.
/// </summary>
/// <param name="item">The clip data item.</param>
/// <param name="owner">The data transfer owning this item.</param>
internal sealed class ClipDataItemToDataTransferItemWrapper(ClipData.Item item, ClipDataToDataTransferWrapper owner)
    : PlatformDataTransferItem
{
    protected override DataFormat[] ProvideFormats()
        => owner.Formats; // There's no "format per item", assume each item handle all formats

    protected override object? TryGetRawCore(DataFormat format)
    {
        if (DataFormat.Text.Equals(format))
            return item.CoerceToText(owner.Context);

        if (format is DataFormat<string>)
            return TryGetString();

        if (DataFormat.File.Equals(format))
            return TryGetStorageItem();

        if (DataFormat.Bitmap.Equals(format))
            return TryGetBitmap();

        if (format is DataFormat<byte[]>)
            return TryGetBytes();

        return null;
    }

    private string? TryGetString()
    {
        if (item.Text is { } text)
            return text;

        if (item.HtmlText is { } htmlText)
            return htmlText;

        if (item.Uri is { } uri)
            return uri.ToString();

        if (item.Intent is { } intent)
            return intent.ToUri(IntentUriType.Scheme);

        return null;
    }

    private IStorageItem? TryGetStorageItem()
        => item.Uri is { Scheme: "file" or "content" } fileUri && owner.Context is Activity activity ?
            AndroidStorageItem.CreateItem(activity, fileUri) :
            null;

    private object? TryGetBitmap()
    {
        try
        {
            if (TryGetStorageItem() is AndroidStorageFile storageFile)
            {
                using var stream = storageFile.OpenRead();

                return new Bitmap(stream);
            }
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.AndroidPlatform)
                ?.Log(this, "Could not get bitmap from clipboard: {Error}", ex.Message);
        }

        return null;
    }

    private object? TryGetBytes()
    {
        try
        {
            if (TryGetStorageItem() is AndroidStorageFile storageFile)
            {
                using var stream = storageFile.OpenRead();

                using var mem = new MemoryStream();
                stream.CopyTo(mem);
                return mem.ToArray();
            }
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.AndroidPlatform)
                ?.Log(this, "Could not get bytes from clipboard: {Error}", ex.Message);
        }

        return null;
    }
}
