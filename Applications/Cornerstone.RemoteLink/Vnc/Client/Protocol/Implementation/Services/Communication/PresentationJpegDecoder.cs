using System;
using System.IO;
using System.Threading;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.RemoteLink.Vnc.Client;
using Cornerstone.RemoteLink.Vnc.Client.Protocol;
using Cornerstone.RemoteLink.Vnc.Client.Protocol.Services;

namespace Cornerstone.RemoteLink.Vnc.Client.Protocol.Implementation.Services.Communication;

/// <summary>
/// JPEG decoder that uses Cornerstone/Skia already loaded by the app. No native TurboJPEG library.
/// </summary>
public sealed class PresentationJpegDecoder : IJpegDecoder
{
	private static readonly PixelFormat BgraCompatiblePixelFormat =
		new PixelFormat("Cornerstone BGRA", 32, 32, false, true, true, 255, 255, 255, 255, 16, 8, 0, 24);

	private static readonly PixelFormat RgbaCompatiblePixelFormat =
		new PixelFormat("Cornerstone RGBA", 32, 32, false, true, true, 255, 255, 255, 255, 0, 8, 16, 24);

	private volatile bool _disposed;

	/// <inheritdoc />
	public unsafe void DecodeJpegTo32Bit(Span<byte> jpegBuffer, Span<byte> pixelsBuffer, int expectedWidth, int expectedHeight, PixelFormat preferredPixelFormat,
		out PixelFormat usedPixelFormat, CancellationToken cancellationToken = default)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(nameof(PresentationJpegDecoder));
		}

		cancellationToken.ThrowIfCancellationRequested();

		fixed (byte* jpegPtr = jpegBuffer)
		{
			using var stream = new UnmanagedMemoryStream(jpegPtr, jpegBuffer.Length, jpegBuffer.Length, FileAccess.Read);
			using var bitmap = new Bitmap(stream);

			if ((bitmap.PixelSize.Width != expectedWidth) || (bitmap.PixelSize.Height != expectedHeight))
			{
				throw new RfbProtocolException(
					$"Cannot decode JPEG image because its size of {bitmap.PixelSize.Width}x{bitmap.PixelSize.Height} does not match the expected size {expectedWidth}x{expectedHeight}.");
			}

			var stride = expectedWidth * 4;
			var requiredLength = stride * expectedHeight;
			if (pixelsBuffer.Length < requiredLength)
			{
				throw new RfbProtocolException(
					$"Cannot decode JPEG image ({expectedWidth}x{expectedHeight}) because its size of {requiredLength} bytes would exceed the pixels buffer size of {pixelsBuffer.Length}.");
			}

			usedPixelFormat = (bitmap.Format == global::Cornerstone.Presentation.Platform.PixelFormat.Rgba8888)
				? RgbaCompatiblePixelFormat
				: BgraCompatiblePixelFormat;

			fixed (byte* pixelsPtr = pixelsBuffer)
			{
				bitmap.CopyPixels(new PixelRect(0, 0, expectedWidth, expectedHeight), (IntPtr)pixelsPtr, pixelsBuffer.Length, stride);
			}
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_disposed = true;
	}
}
