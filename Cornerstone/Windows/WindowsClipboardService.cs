#region References

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cornerstone.Internal;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Windows;

/// <summary>
/// The clipboard service for the Windows environment.
/// </summary>
/// <remarks>
/// Copied from Avalonia.Win32.ClipboardImpl : IClipboard
/// </remarks>
public class WindowsClipboardService : IClipboardService
{
	#region Constants

	private const int _oleRetryCount = 10;

	/// <summary>
	/// Unicode text format. Each line ends with a carriage return/linefeed (CR-LF) combination. A null character signals the end of the data.
	/// </summary>
	private const int _unicodeTextFormat = 13;

	#endregion

	#region Methods

	/// <inheritdoc />
	public async Task ClearAsync()
	{
		using (await OpenClipboard())
		{
			EmptyClipboard();
		}
	}

	/// <inheritdoc />
	public async Task<string> GetTextAsync()
	{
		using (await OpenClipboard())
		{
			var hText = GetClipboardData(_unicodeTextFormat);
			if (hText == IntPtr.Zero)
			{
				return null;
			}

			var pText = GlobalLock(hText);
			if (pText == IntPtr.Zero)
			{
				return null;
			}

			var rv = Marshal.PtrToStringUni(pText);
			GlobalUnlock(hText);
			return rv;
		}
	}

	/// <inheritdoc />
	public async Task SetTextAsync(string text)
	{
		using (await OpenClipboard())
		{
			EmptyClipboard();

			if (text is not null)
			{
				var hGlobal = Marshal.StringToHGlobalUni(text);
				SetClipboardData(_unicodeTextFormat, hGlobal);
			}
		}
	}

	[DllImport("user32.dll")]
	private static extern bool CloseClipboard();

	[DllImport("user32.dll")]
	private static extern bool Empty­Clipboard();

	[DllImport("user32.dll")]
	private static extern IntPtr GetClipboardData(int format);

	[DllImport("kernel32.dll", ExactSpelling = true)]
	private static extern IntPtr GlobalLock(IntPtr handle);

	[DllImport("kernel32.dll", ExactSpelling = true)]
	private static extern bool GlobalUnlock(IntPtr handle);

	private static async Task<IDisposable> OpenClipboard()
	{
		var i = _oleRetryCount;

		while (!OpenClipboard(IntPtr.Zero))
		{
			if (--i == 0)
			{
				throw new TimeoutException("Timeout opening clipboard.");
			}
			await Task.Delay(100);
		}

		return Disposable.Create(() => CloseClipboard());
	}

	[DllImport("user32.dll")]
	private static extern bool OpenClipboard(IntPtr owner);

	[DllImport("user32.dll")]
	private static extern bool SetClipboardData(uint format, IntPtr data);

	#endregion
}