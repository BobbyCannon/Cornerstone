#region References

using System;
using System.Runtime.InteropServices;
using System.Text;

#endregion

namespace Cornerstone.Platforms.Windows;

public static class Clipboard
{
	#region Constants

	private const uint CfUnicodeText = 13;

	#endregion

	#region Methods

	public static void SetText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		if (!OpenClipboard(IntPtr.Zero))
		{
			throw new Exception("Failed to open clipboard");
		}

		try
		{
			EmptyClipboard();

			// Allocate memory for Unicode text (+ null terminator)
			var bytes = Encoding.Unicode.GetBytes(text + "\0");
			var hGlobal = GlobalAlloc(0x2000 /* GMEM_MOVEABLE */, (UIntPtr) bytes.Length);

			if (hGlobal == IntPtr.Zero)
			{
				throw new OutOfMemoryException();
			}

			var locked = GlobalLock(hGlobal);
			if (locked != IntPtr.Zero)
			{
				Marshal.Copy(bytes, 0, locked, bytes.Length);
				GlobalUnlock(hGlobal);

				if (SetClipboardData(CfUnicodeText, hGlobal) == IntPtr.Zero)
				{
					throw new Exception("Failed to set clipboard data");
				}
			}
		}
		finally
		{
			CloseClipboard();
		}
	}

	[DllImport("user32.dll")]
	private static extern bool CloseClipboard();

	[DllImport("user32.dll")]
	private static extern bool EmptyClipboard();

	[DllImport("kernel32.dll")]
	private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GlobalLock(IntPtr hMem);

	[DllImport("kernel32.dll")]
	private static extern bool GlobalUnlock(IntPtr hMem);

	[DllImport("user32.dll")]
	private static extern bool OpenClipboard(IntPtr hWndNewOwner);

	[DllImport("user32.dll")]
	private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

	#endregion
}