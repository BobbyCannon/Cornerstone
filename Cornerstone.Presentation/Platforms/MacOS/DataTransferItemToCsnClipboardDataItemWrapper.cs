using System;
using System.IO;
using System.Linq;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platforms.MacOS.Interop;

namespace Cornerstone.Presentation.Platforms.MacOS;

/// <summary>
/// Wraps a <see cref="IDataTransferItem"/> into a <see cref="ICsnClipboardDataItem"/>.
/// This class is called by native code.
/// </summary>
/// <param name="item">The item to wrap.</param>
internal sealed class DataTransferItemToCsnClipboardDataItemWrapper(IDataTransferItem item)
    : NativeOwned, ICsnClipboardDataItem
{
    private readonly IDataTransferItem _item = item;

    ICsnStringArray ICsnClipboardDataItem.ProvideFormats()
        => new CsnStringArray(_item.Formats.Where(f => f.Kind != DataFormatKind.InProcess).Select(ClipboardDataFormatHelper.ToNativeFormat));

    ICsnClipboardDataValue? ICsnClipboardDataItem.GetValue(string format)
    {
        if (FindDataFormat(format) is { } dataFormat)
        {
            if (DataFormat.Text.Equals(dataFormat))
                return new StringValue(_item.TryGetValue(DataFormat.Text) ?? string.Empty);

            if (DataFormat.File.Equals(dataFormat))
                return _item.TryGetValue(DataFormat.File) is { } file ? new StringValue(file.Path.AbsoluteUri) : null;

            if (DataFormat.Bitmap.Equals(dataFormat))
            {
                if (_item.TryGetValue(DataFormat.Bitmap) is { } bitmap)
                {
                    var memoryStream = new MemoryStream();
                    bitmap.Save(memoryStream, PngBitmapEncoderOptions.Default);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return new StreamValue(memoryStream);
                }

                return null;
            }

            if (dataFormat is DataFormat<string> stringFormat)
                return _item.TryGetValue(stringFormat) is { } stringValue ? new StringValue(stringValue) : null;

            if (dataFormat is DataFormat<byte[]> bytesFormat)
                return _item.TryGetValue(bytesFormat) is { } bytes ? new BytesValue(bytes) : null;
        }

        Logger.TryGet(LogEventLevel.Warning, LogArea.macOSPlatform)
            ?.Log(this, "Unsupported data format {Format}", format);

        return null;
    }

    private DataFormat? FindDataFormat(string nativeFormat)
    {
        var formats = _item.Formats;
        var count = formats.Count;
        for (var i = 0; i < count; i++)
        {
            var format = formats[i];
            if (format.Kind == DataFormatKind.InProcess)
                continue;
            if (ClipboardDataFormatHelper.ToNativeFormat(format) == nativeFormat)
                return format;
        }

        return null;
    }

    private sealed class StringValue(string value) : NativeOwned, ICsnClipboardDataValue
    {
        private readonly string _value = value;

        int ICsnClipboardDataValue.IsString()
            => true.AsComBool();

        ICsnString ICsnClipboardDataValue.AsString()
            => new CsnString(_value);

        IntPtr ICsnClipboardDataValue.ByteLength
            => throw new InvalidOperationException();

        unsafe void ICsnClipboardDataValue.CopyBytesTo(void* buffer)
            => throw new InvalidOperationException();
    }

    private sealed class BytesValue(ReadOnlyMemory<byte> value) : NativeOwned, ICsnClipboardDataValue
    {
        private readonly ReadOnlyMemory<byte> _value = value;

        int ICsnClipboardDataValue.IsString()
            => false.AsComBool();

        ICsnString ICsnClipboardDataValue.AsString()
            => throw new InvalidOperationException();

        IntPtr ICsnClipboardDataValue.ByteLength
            => new(_value.Length);

        unsafe void ICsnClipboardDataValue.CopyBytesTo(void* buffer)
            => _value.Span.CopyTo(new Span<byte>(buffer, _value.Length));
    }
    
    private sealed class StreamValue(MemoryStream value) : NativeOwned, ICsnClipboardDataValue
    {
        private readonly MemoryStream _value = value;
        private readonly byte[] _buffer = new byte[1024 * 1024];

        int ICsnClipboardDataValue.IsString()
            => false.AsComBool();

        ICsnString ICsnClipboardDataValue.AsString()
            => throw new InvalidOperationException();

        IntPtr ICsnClipboardDataValue.ByteLength
#pragma warning disable CA2020 // overflow in unchecked context
            => (IntPtr)_value.Length;
#pragma warning restore CA2020

        unsafe void ICsnClipboardDataValue.CopyBytesTo(void* output)
        {
            long totalCopied = 0;

            while (true)
            {
                var read = _value.Read(_buffer, 0, _buffer.Length);
                if (read == 0)
                    break;

                var destinationSpan = new Span<byte>((byte*)output + totalCopied, read);
                _buffer.AsSpan(0, read).CopyTo(destinationSpan);

                totalCopied += read;
            }
        }
    }
}
