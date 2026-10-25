#region References

using System;
using System.Runtime.InteropServices;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Platforms.Linux;

/// <summary>
/// X11 screen sample. Wayland without an X display cannot be read this way.
/// The XImage field offsets are the 64-bit libX11 layout.
/// </summary>
[SourceReflection]
public sealed class LinuxScreenSampler : IScreenSampler
{
	#region Constants

	private const int DestroyImageOffset = 96;
	private const int ZPixmap = 2;

	#endregion

	#region Fields

	private readonly object _gate;
	private IntPtr _display;
	private bool _probed;
	private IntPtr _root;
	private int _screen;

	#endregion

	#region Constructors

	public LinuxScreenSampler()
	{
		_gate = new object();
		_display = IntPtr.Zero;
		_probed = false;
		_root = IntPtr.Zero;
		_screen = 0;
	}

	#endregion

	#region Properties

	public bool IsSupported
	{
		get
		{
			if (!OperatingSystem.IsLinux() || !Environment.Is64BitProcess)
			{
				return false;
			}

			lock (_gate)
			{
				return EnsureDisplay();
			}
		}
	}

	public string UnavailableReason => IsSupported ? string.Empty : "Screen sampling needs an X11 display.";

	#endregion

	#region Methods

	public bool TryCopySample(int sampleSize, byte[] bgraPixels, out byte alpha, out byte red, out byte green, out byte blue)
	{
		alpha = 0;
		red = 0;
		green = 0;
		blue = 0;
		if (!OperatingSystem.IsLinux()
			|| !Environment.Is64BitProcess
			|| (sampleSize <= 0)
			|| (bgraPixels == null)
			|| (bgraPixels.Length < (sampleSize * sampleSize * 4)))
		{
			return false;
		}

		lock (_gate)
		{
			if (!EnsureDisplay())
			{
				return false;
			}

			if (XQueryPointer(_display, _root, out _, out _, out var rootX, out var rootY, out _, out _, out _) == 0)
			{
				return false;
			}

			var originX = rootX - (sampleSize / 2);
			var originY = rootY - (sampleSize / 2);
			var screenWidth = XDisplayWidth(_display, _screen);
			var screenHeight = XDisplayHeight(_display, _screen);
			var maxX = screenWidth - sampleSize;
			var maxY = screenHeight - sampleSize;
			if (maxX < 0)
			{
				maxX = 0;
			}

			if (maxY < 0)
			{
				maxY = 0;
			}

			if (originX < 0)
			{
				originX = 0;
			}
			else if (originX > maxX)
			{
				originX = maxX;
			}

			if (originY < 0)
			{
				originY = 0;
			}
			else if (originY > maxY)
			{
				originY = maxY;
			}

			var image = XGetImage(_display, _root, originX, originY, (uint) sampleSize, (uint) sampleSize, ulong.MaxValue, ZPixmap);
			if (image == IntPtr.Zero)
			{
				return false;
			}

			try
			{
				return ReadImage(image, sampleSize, bgraPixels, out alpha, out red, out green, out blue);
			}
			finally
			{
				DestroyImage(image);
			}
		}
	}

	private bool EnsureDisplay()
	{
		if (_probed)
		{
			return _display != IntPtr.Zero;
		}

		_probed = true;
		XInitThreads();
		_display = XOpenDisplay(IntPtr.Zero);
		if (_display == IntPtr.Zero)
		{
			return false;
		}

		_screen = XDefaultScreen(_display);
		_root = XRootWindow(_display, _screen);
		return _root != IntPtr.Zero;
	}

	private static void DestroyImage(IntPtr image)
	{
		var destroy = Marshal.ReadIntPtr(image, DestroyImageOffset);
		if (destroy == IntPtr.Zero)
		{
			return;
		}

		Marshal.GetDelegateForFunctionPointer<DestroyImageDelegate>(destroy)(image);
	}

	private static byte MaskChannel(uint pixel, ulong mask)
	{
		if (mask == 0)
		{
			return 255;
		}

		var shift = 0;
		while ((((mask >> shift) & 1) == 0) && (shift < 63))
		{
			shift++;
		}

		var bits = 0;
		var probe = mask >> shift;
		while ((probe & 1) == 1)
		{
			bits++;
			probe >>= 1;
		}

		if (bits <= 0)
		{
			return 0;
		}

		var raw = (uint) ((pixel & (uint) mask) >> shift);
		if (bits >= 8)
		{
			return (byte) (raw >> (bits - 8));
		}

		return (byte) ((raw * 255) / ((1 << bits) - 1));
	}

	private static bool ReadImage(IntPtr image, int sampleSize, byte[] bgraPixels, out byte alpha, out byte red, out byte green, out byte blue)
	{
		alpha = 0;
		red = 0;
		green = 0;
		blue = 0;
		var data = Marshal.ReadIntPtr(image, 16);
		var bytesPerLine = Marshal.ReadInt32(image, 44);
		var bitsPerPixel = Marshal.ReadInt32(image, 48);
		var redMask = (ulong) Marshal.ReadInt64(image, 56);
		var greenMask = (ulong) Marshal.ReadInt64(image, 64);
		var blueMask = (ulong) Marshal.ReadInt64(image, 72);
		if ((data == IntPtr.Zero) || (bytesPerLine <= 0) || ((bitsPerPixel != 24) && (bitsPerPixel != 32)))
		{
			return false;
		}

		var bytesPerPixel = bitsPerPixel / 8;
		for (var y = 0; y < sampleSize; y++)
		{
			for (var x = 0; x < sampleSize; x++)
			{
				var source = data + (y * bytesPerLine) + (x * bytesPerPixel);
				uint pixel;
				if (bitsPerPixel == 32)
				{
					pixel = (uint) Marshal.ReadInt32(source);
				}
				else
				{
					pixel = (uint) (Marshal.ReadByte(source)
						| (Marshal.ReadByte(source, 1) << 8)
						| (Marshal.ReadByte(source, 2) << 16));
				}

				var pixelBlue = MaskChannel(pixel, blueMask);
				var pixelGreen = MaskChannel(pixel, greenMask);
				var pixelRed = MaskChannel(pixel, redMask);
				var offset = ((y * sampleSize) + x) * 4;
				bgraPixels[offset] = pixelBlue;
				bgraPixels[offset + 1] = pixelGreen;
				bgraPixels[offset + 2] = pixelRed;
				bgraPixels[offset + 3] = 255;
				if ((x == (sampleSize / 2)) && (y == (sampleSize / 2)))
				{
					blue = pixelBlue;
					green = pixelGreen;
					red = pixelRed;
					alpha = 255;
				}
			}
		}

		return true;
	}

	[DllImport("libX11.so.6")]
	private static extern int XDefaultScreen(IntPtr display);

	[DllImport("libX11.so.6")]
	private static extern int XDisplayHeight(IntPtr display, int screen);

	[DllImport("libX11.so.6")]
	private static extern int XDisplayWidth(IntPtr display, int screen);

	[DllImport("libX11.so.6")]
	private static extern IntPtr XGetImage(IntPtr display, IntPtr drawable, int x, int y, uint width, uint height, ulong planeMask, int format);

	[DllImport("libX11.so.6")]
	private static extern int XInitThreads();

	[DllImport("libX11.so.6")]
	private static extern IntPtr XOpenDisplay(IntPtr display);

	[DllImport("libX11.so.6")]
	private static extern int XQueryPointer(IntPtr display, IntPtr window, out IntPtr rootReturn, out IntPtr childReturn, out int rootX, out int rootY, out int windowX, out int windowY, out int mask);

	[DllImport("libX11.so.6")]
	private static extern IntPtr XRootWindow(IntPtr display, int screen);

	#endregion

	#region Nested Types

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int DestroyImageDelegate(IntPtr image);

	#endregion
}
