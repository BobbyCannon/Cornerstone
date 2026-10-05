#region References

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ProtocolPixelFormat = Cornerstone.Presentation.Remote.Protocol.Viewport.PixelFormat;

#endregion

namespace Cornerstone.Presentation.Remote.Wpf;

/// <summary>
/// Writes a remote frame into a WPF WriteableBitmap. Call on the UI thread.
/// </summary>
public static class RemoteFrameBitmap
{
	#region Methods

	public static WriteableBitmap Apply(WriteableBitmap bitmap, RemoteFrame frame)
	{
		if (frame == null)
		{
			return bitmap;
		}

		var sizeChanged = (bitmap == null) ||
			(bitmap.PixelWidth != frame.Width) ||
			(bitmap.PixelHeight != frame.Height);

		if (sizeChanged)
		{
			bitmap = new WriteableBitmap(
				Math.Max(frame.Width, 1),
				Math.Max(frame.Height, 1),
				96,
				96,
				ToWpf(frame.Format),
				null);
		}

		if ((frame.Width > 0) && (frame.Height > 0) && (frame.Data != null))
		{
			bitmap.WritePixels(
				new Int32Rect(0, 0, frame.Width, frame.Height),
				frame.Data,
				frame.Stride,
				0);
		}

		return bitmap;
	}

	public static PixelFormat ToWpf(ProtocolPixelFormat format)
	{
		switch (format)
		{
			case ProtocolPixelFormat.Bgra8888:
				return PixelFormats.Bgra32;
			case ProtocolPixelFormat.Rgb565:
				return PixelFormats.Bgr565;
			case ProtocolPixelFormat.Rgba8888:
				return PixelFormats.Pbgra32;
			default:
				throw new NotSupportedException("Unsupported pixel format.");
		}
	}

	#endregion
}