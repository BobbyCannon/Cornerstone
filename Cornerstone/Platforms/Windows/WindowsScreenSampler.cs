#region References

using System;
using System.Runtime.InteropServices;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Platforms.Windows;

/// <summary>
/// GDI screen sample. BI_RGB leaves alpha at 0, so captured pixels are treated as opaque.
/// </summary>
[SourceReflection]
public sealed class WindowsScreenSampler : IScreenSampler
{
	#region Constants

	private const int CaptureBlt = 0x40000000;
	private const int SrcCopy = 0x00CC0020;

	#endregion

	#region Properties

	public bool IsSupported => OperatingSystem.IsWindows();

	public string UnavailableReason => string.Empty;

	#endregion

	#region Methods

	public bool TryCopySample(int sampleSize, byte[] bgraPixels, out byte alpha, out byte red, out byte green, out byte blue)
	{
		alpha = 0;
		red = 0;
		green = 0;
		blue = 0;
		if (!OperatingSystem.IsWindows()
			|| (sampleSize <= 0)
			|| (bgraPixels == null)
			|| (bgraPixels.Length < (sampleSize * sampleSize * 4)))
		{
			return false;
		}

		if (!GetCursorPos(out var point))
		{
			return false;
		}

		var desktop = GetDesktopWindow();
		var screenDc = GetWindowDC(desktop);
		if (screenDc == IntPtr.Zero)
		{
			return false;
		}

		var memoryDc = IntPtr.Zero;
		var bitmap = IntPtr.Zero;
		var previous = IntPtr.Zero;
		try
		{
			memoryDc = CreateCompatibleDC(screenDc);
			bitmap = CreateCompatibleBitmap(screenDc, sampleSize, sampleSize);
			if ((memoryDc == IntPtr.Zero) || (bitmap == IntPtr.Zero))
			{
				return false;
			}

			previous = SelectObject(memoryDc, bitmap);
			var originX = point.X - (sampleSize / 2);
			var originY = point.Y - (sampleSize / 2);
			if (!BitBlt(memoryDc, 0, 0, sampleSize, sampleSize, screenDc, originX, originY, SrcCopy | CaptureBlt))
			{
				return false;
			}

			SelectObject(memoryDc, previous);
			previous = IntPtr.Zero;

			var info = new BitmapInfo();
			info.Header.Size = 40;
			info.Header.Width = sampleSize;
			info.Header.Height = -sampleSize;
			info.Header.Planes = 1;
			info.Header.BitCount = 32;
			if (GetDIBits(screenDc, bitmap, 0, (uint) sampleSize, bgraPixels, ref info, 0) == 0)
			{
				return false;
			}

			for (var index = 3; index < bgraPixels.Length; index += 4)
			{
				bgraPixels[index] = 255;
			}

			var center = sampleSize / 2;
			var offset = ((center * sampleSize) + center) * 4;
			blue = bgraPixels[offset];
			green = bgraPixels[offset + 1];
			red = bgraPixels[offset + 2];
			alpha = bgraPixels[offset + 3];
			return true;
		}
		finally
		{
			if ((previous != IntPtr.Zero) && (memoryDc != IntPtr.Zero))
			{
				SelectObject(memoryDc, previous);
			}

			if (bitmap != IntPtr.Zero)
			{
				DeleteObject(bitmap);
			}

			if (memoryDc != IntPtr.Zero)
			{
				DeleteDC(memoryDc);
			}

			ReleaseDC(desktop, screenDc);
		}
	}

	[DllImport("gdi32.dll")]
	private static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, int operation);

	[DllImport("gdi32.dll")]
	private static extern IntPtr CreateCompatibleBitmap(IntPtr deviceContext, int width, int height);

	[DllImport("gdi32.dll")]
	private static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(IntPtr deviceContext);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(IntPtr @object);

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out Point point);

	[DllImport("gdi32.dll")]
	private static extern int GetDIBits(IntPtr deviceContext, IntPtr bitmap, uint start, uint lines, byte[] bits, ref BitmapInfo info, uint usage);

	[DllImport("user32.dll")]
	private static extern IntPtr GetDesktopWindow();

	[DllImport("user32.dll")]
	private static extern IntPtr GetWindowDC(IntPtr window);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(IntPtr window, IntPtr deviceContext);

	[DllImport("gdi32.dll")]
	private static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr @object);

	#endregion

	#region Structures

	[StructLayout(LayoutKind.Sequential)]
	private struct BitmapInfo
	{
		public BitmapInfoHeader Header;
		public uint Colors;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct BitmapInfoHeader
	{
		public int Size;
		public int Width;
		public int Height;
		public short Planes;
		public short BitCount;
		public int Compression;
		public int SizeImage;
		public int XPixelsPerMeter;
		public int YPixelsPerMeter;
		public int ColorsUsed;
		public int ImportantColors;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Point
	{
		public int X;
		public int Y;
	}

	#endregion
}