#region References

using System;
using System.Runtime.InteropServices;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Platforms.MacOS;

/// <summary>
/// Core Graphics screen sample. macOS may require Screen Recording before a capture returns pixels.
/// </summary>
[SourceReflection]
public sealed class MacOSScreenSampler : IScreenSampler
{
	#region Constants

	private const int AlphaFirst = 4;
	private const int AlphaInfoMask = 0x1F;
	private const int AlphaLast = 3;
	private const int AlphaNoneSkipFirst = 6;
	private const int AlphaNoneSkipLast = 5;
	private const int AlphaPremultipliedFirst = 2;
	private const int AlphaPremultipliedLast = 1;
	private const uint ByteOrder32Little = 2 << 12;
	private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
	private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
	private const uint OnScreenOnly = 1;
	private const uint ShouldBeOpaque = 2;

	#endregion

	#region Properties

	public bool IsSupported => OperatingSystem.IsMacOS();

	public string UnavailableReason => string.Empty;

	#endregion

	#region Methods

	public bool TryCopySample(int sampleSize, byte[] bgraPixels, out byte alpha, out byte red, out byte green, out byte blue)
	{
		alpha = 0;
		red = 0;
		green = 0;
		blue = 0;
		if (!OperatingSystem.IsMacOS()
			|| (sampleSize <= 0)
			|| (bgraPixels == null)
			|| (bgraPixels.Length < (sampleSize * sampleSize * 4)))
		{
			return false;
		}

		var created = CGEventCreate(IntPtr.Zero);
		if (created == IntPtr.Zero)
		{
			return false;
		}

		CgPoint location;
		try
		{
			location = CGEventGetLocation(created);
		}
		finally
		{
			CFRelease(created);
		}

		var half = sampleSize / 2.0;
		var center = sampleSize / 2;
		var bounds = new CgRect
		{
			Origin = new CgPoint
			{
				X = location.X - half,
				Y = location.Y - half
			},
			Size = new CgSize
			{
				Width = sampleSize,
				Height = sampleSize
			}
		};
		var image = CGWindowListCreateImage(bounds, OnScreenOnly, 0, ShouldBeOpaque);
		if (image == IntPtr.Zero)
		{
			return false;
		}

		var data = IntPtr.Zero;
		try
		{
			var width = (int) CGImageGetWidth(image);
			var height = (int) CGImageGetHeight(image);
			var bytesPerRow = (int) CGImageGetBytesPerRow(image);
			var bitsPerPixel = CGImageGetBitsPerPixel(image);
			if ((width < sampleSize) || (height < sampleSize) || (bitsPerPixel != 32) || (bytesPerRow < (width * 4)))
			{
				return false;
			}

			var provider = CGImageGetDataProvider(image);
			if (provider == IntPtr.Zero)
			{
				return false;
			}

			data = CGDataProviderCopyData(provider);
			if (data == IntPtr.Zero)
			{
				return false;
			}

			var bytes = CFDataGetBytePtr(data);
			if (bytes == IntPtr.Zero)
			{
				return false;
			}

			var info = CGImageGetBitmapInfo(image);
			var littleEndian = (info & ByteOrder32Little) == ByteOrder32Little;
			var alphaInfo = (int) (info & AlphaInfoMask);
			for (var y = 0; y < sampleSize; y++)
			{
				for (var x = 0; x < sampleSize; x++)
				{
					var source = bytes + (y * bytesPerRow) + (x * 4);
					UnpackPixel(source, littleEndian, alphaInfo, out var pixelAlpha, out var pixelRed, out var pixelGreen, out var pixelBlue);
					var offset = ((y * sampleSize) + x) * 4;
					bgraPixels[offset] = pixelBlue;
					bgraPixels[offset + 1] = pixelGreen;
					bgraPixels[offset + 2] = pixelRed;
					bgraPixels[offset + 3] = pixelAlpha;
					if ((x == center) && (y == center))
					{
						alpha = pixelAlpha;
						red = pixelRed;
						green = pixelGreen;
						blue = pixelBlue;
					}
				}
			}

			return true;
		}
		finally
		{
			if (data != IntPtr.Zero)
			{
				CFRelease(data);
			}

			CFRelease(image);
		}
	}

	private static void UnpackPixel(IntPtr source, bool littleEndian, int alphaInfo, out byte alpha, out byte red, out byte green, out byte blue)
	{
		var b0 = Marshal.ReadByte(source);
		var b1 = Marshal.ReadByte(source, 1);
		var b2 = Marshal.ReadByte(source, 2);
		var b3 = Marshal.ReadByte(source, 3);
		if (littleEndian && ((alphaInfo == AlphaPremultipliedFirst) || (alphaInfo == AlphaFirst) || (alphaInfo == AlphaNoneSkipFirst)))
		{
			blue = b0;
			green = b1;
			red = b2;
			alpha = (alphaInfo == AlphaNoneSkipFirst) ? (byte) 255 : b3;
		}
		else if (!littleEndian && ((alphaInfo == AlphaPremultipliedLast) || (alphaInfo == AlphaLast) || (alphaInfo == AlphaNoneSkipLast)))
		{
			red = b0;
			green = b1;
			blue = b2;
			alpha = (alphaInfo == AlphaNoneSkipLast) ? (byte) 255 : b3;
		}
		else if (littleEndian && ((alphaInfo == AlphaPremultipliedLast) || (alphaInfo == AlphaLast) || (alphaInfo == AlphaNoneSkipLast)))
		{
			red = b0;
			green = b1;
			blue = b2;
			alpha = (alphaInfo == AlphaNoneSkipLast) ? (byte) 255 : b3;
		}
		else
		{
			blue = b0;
			green = b1;
			red = b2;
			alpha = 255;
		}

		if ((alpha > 0) && (alpha < 255)
			&& ((alphaInfo == AlphaPremultipliedFirst) || (alphaInfo == AlphaPremultipliedLast)))
		{
			red = (byte) ((red * 255) / alpha);
			green = (byte) ((green * 255) / alpha);
			blue = (byte) ((blue * 255) / alpha);
		}
	}

	[DllImport(CoreFoundation)]
	private static extern IntPtr CFDataGetBytePtr(IntPtr data);

	[DllImport(CoreFoundation)]
	private static extern void CFRelease(IntPtr value);

	[DllImport(CoreGraphics)]
	private static extern IntPtr CGDataProviderCopyData(IntPtr provider);

	[DllImport(CoreGraphics)]
	private static extern IntPtr CGEventCreate(IntPtr source);

	[DllImport(CoreGraphics)]
	private static extern CgPoint CGEventGetLocation(IntPtr eventRef);

	[DllImport(CoreGraphics)]
	private static extern uint CGImageGetBitmapInfo(IntPtr image);

	[DllImport(CoreGraphics)]
	private static extern int CGImageGetBitsPerPixel(IntPtr image);

	[DllImport(CoreGraphics)]
	private static extern nint CGImageGetBytesPerRow(IntPtr image);

	[DllImport(CoreGraphics)]
	private static extern IntPtr CGImageGetDataProvider(IntPtr image);

	[DllImport(CoreGraphics)]
	private static extern nint CGImageGetHeight(IntPtr image);

	[DllImport(CoreGraphics)]
	private static extern nint CGImageGetWidth(IntPtr image);

	[DllImport(CoreGraphics)]
	private static extern IntPtr CGWindowListCreateImage(CgRect screenBounds, uint listOption, uint windowId, uint imageOption);

	#endregion

	#region Nested Types

	[StructLayout(LayoutKind.Sequential)]
	private struct CgPoint
	{
		public double X;
		public double Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct CgRect
	{
		public CgPoint Origin;
		public CgSize Size;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct CgSize
	{
		public double Width;
		public double Height;
	}

	#endregion
}
