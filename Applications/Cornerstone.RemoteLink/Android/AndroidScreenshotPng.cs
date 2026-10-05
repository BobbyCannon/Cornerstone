#region References

using System;
using System.Runtime.InteropServices;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Media.Imaging;

#endregion

namespace Cornerstone.RemoteLink.Android;

/// <summary>
/// Decodes adb exec-out screencap -p (PNG, possibly with a junk prefix).
/// </summary>
public static class AndroidScreenshotPng
{
	#region Fields

	private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

	#endregion

	#region Methods

	public static bool TryDecode(byte[] data, out byte[] bgra, out int width, out int height)
	{
		bgra = null;
		width = 0;
		height = 0;
		var offset = IndexOfPng(data);
		if (offset < 0)
		{
			return false;
		}

		try
		{
			using var stream = new System.IO.MemoryStream(data, offset, data.Length - offset, false);
			using var bitmap = new Bitmap(stream);
			width = bitmap.PixelSize.Width;
			height = bitmap.PixelSize.Height;
			if ((width <= 0) || (height <= 0))
			{
				return false;
			}

			var stride = width * 4;
			bgra = new byte[stride * height];
			var handle = GCHandle.Alloc(bgra, GCHandleType.Pinned);
			try
			{
				bitmap.CopyPixels(new PixelRect(0, 0, width, height), handle.AddrOfPinnedObject(), bgra.Length, stride);
			}
			finally
			{
				handle.Free();
			}

			return true;
		}
		catch
		{
			bgra = null;
			width = 0;
			height = 0;
			return false;
		}
	}

	private static int IndexOfPng(byte[] data)
	{
		if ((data == null) || (data.Length < PngSignature.Length))
		{
			return -1;
		}

		var last = data.Length - PngSignature.Length;
		for (var i = 0; i <= last; i++)
		{
			var match = true;
			for (var n = 0; n < PngSignature.Length; n++)
			{
				if (data[i + n] != PngSignature[n])
				{
					match = false;
					break;
				}
			}

			if (match)
			{
				return i;
			}
		}

		return -1;
	}

	#endregion
}
