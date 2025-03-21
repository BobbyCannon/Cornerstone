#region References

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Internal;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Windows;

/// <summary>
/// The clipboard service for the Windows environment.
/// </summary>
/// <remarks>
/// Copied from Avalonia.Win32.ClipboardImpl : IClipboard
/// </remarks>
public class WindowsClipboardService : IClipboardService, IDisposable
{
	#region Constants

	private const int OleRetryCount = 10;

	/// <summary>
	/// Unicode text format. Each line ends with a carriage return/linefeed (CR-LF) combination. A null character signals the end of the data.
	/// </summary>
	private const int UnicodeTextFormat = 13;

	#endregion

	#region Fields

	private readonly DebounceService _clearDebounce;

	#endregion

	#region Constructors

	public WindowsClipboardService(IDateTimeProvider dateTimeProvider)
	{
		_clearDebounce = new DebounceService(TimeSpan.FromSeconds(10), _ => ClearAsync(), dateTimeProvider);
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public Task ClearAsync()
	{
		using var clipboard = OpenClipboard();
		EmptyClipboard();
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		_clearDebounce?.Dispose();
	}

	/// <inheritdoc />
	public Task<string> GetTextAsync()
	{
		using var clipboard = OpenClipboard();
		var hText = GetClipboardData(UnicodeTextFormat);
		if (hText == IntPtr.Zero)
		{
			return Task.FromResult<string>(null);
		}

		var pText = GlobalLock(hText);
		if (pText == IntPtr.Zero)
		{
			return Task.FromResult<string>(null);
		}

		var rv = Marshal.PtrToStringUni(pText);
		GlobalUnlock(hText);
		return Task.FromResult(rv);
	}

	public void QueueClear()
	{
		_clearDebounce.Trigger();
	}

	/// <inheritdoc />
	public Task SetTextAsync(string text)
	{
		using var clipboard = OpenClipboard();
		EmptyClipboard();

		if (text is not null)
		{
			var hGlobal = Marshal.StringToHGlobalUni(text);
			SetClipboardData(UnicodeTextFormat, hGlobal);
		}

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task SetTextAsync(SecureString text)
	{
		using var clipboard = OpenClipboard();
		EmptyClipboard();

		if (text is not null)
		{
			var hGlobal = Marshal.StringToHGlobalUni(text.ToUnsecureString());
			SetClipboardData(UnicodeTextFormat, hGlobal);
		}

		return Task.CompletedTask;
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

	private static IDisposable OpenClipboard()
	{
		var i = OleRetryCount;

		while (!OpenClipboard(IntPtr.Zero))
		{
			if (--i == 0)
			{
				throw new TimeoutException("Timeout opening clipboard.");
			}
			Thread.Sleep(100);
		}

		return Disposable.Create(() => CloseClipboard());
	}

	[DllImport("user32.dll")]
	private static extern bool OpenClipboard(IntPtr owner);

	[DllImport("user32.dll")]
	private static extern bool SetClipboardData(uint format, IntPtr data);

	#endregion
}