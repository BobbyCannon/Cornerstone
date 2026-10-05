using System;
using Cornerstone.Presentation;
using Cornerstone.RemoteLink.Vnc.Client;
using PixelFormat = Cornerstone.RemoteLink.Vnc.Client.PixelFormat;
using Size = Cornerstone.RemoteLink.Vnc.Client.Size;

namespace Cornerstone.RemoteLink.Vnc.Controls;

/// <summary>
/// Helper functions for converting Cornerstone types to RFB types.
/// </summary>
public static class Conversions
{
	public static Size GetSize(PixelSize pixelSize)
	{
		return new Size(pixelSize.Width, pixelSize.Height);
	}

	public static PixelSize GetPixelSize(Size size)
	{
		return new PixelSize(size.Width, size.Height);
	}

	public static PixelFormat GetPixelFormat(global::Cornerstone.Presentation.Platform.PixelFormat pixelFormat)
	{
		if (pixelFormat == global::Cornerstone.Presentation.Platform.PixelFormat.Rgb565)
		{
			return new PixelFormat("Skia kRGB_565_SkColorType", 16, 16, false, true, false, 31, 63, 31, 0, 11, 5, 0, 0);
		}

		if (pixelFormat == global::Cornerstone.Presentation.Platform.PixelFormat.Rgba8888)
		{
			return new PixelFormat("Skia kRGBA_8888_SkColorType", 32, 32, false, true, true, 0xFF, 0xFF, 0xFF, 0xFF, 0, 8, 16, 24);
		}

		if (pixelFormat == global::Cornerstone.Presentation.Platform.PixelFormat.Bgra8888)
		{
			return new PixelFormat("Skia kBGRA_8888_SkColorType", 32, 32, false, true, true, 0xFF, 0xFF, 0xFF, 0xFF, 16, 8, 0, 24);
		}

		throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, "Unsupported pixel format.");
	}

	public static Position GetPosition(Point point)
	{
		return new Position((int)point.X, (int)point.Y);
	}
}
