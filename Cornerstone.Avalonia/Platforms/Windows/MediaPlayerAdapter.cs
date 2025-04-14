#region References

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Cornerstone.Avalonia.MediaPlayer;

#endregion

namespace Cornerstone.Avalonia.Platforms.Windows;

internal class MediaPlayerAdapter : IMediaPlayerAdapter
{
	#region Fields

	private string _currentUrl;
	private IntPtr _mediaPlayer;
	private NativeControlHost _nativeHost;

	#endregion

	#region Constructors

	public MediaPlayerAdapter()
	{
		var hr = MFStartup(0x10070, 0);
		if (hr != 0)
		{
			throw new Exception($"MFStartup failed: HRESULT {hr:X8}");
		}
	}

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_mediaPlayer != IntPtr.Zero)
		{
			IMFPMediaPlayer_Stop(_mediaPlayer);
			Marshal.Release(_mediaPlayer);
			_mediaPlayer = IntPtr.Zero;
			_currentUrl = null;
		}

		MFShutdown();
	}

	public void Initialize(NativeControlHost nativeHost)
	{
		_nativeHost = nativeHost ?? throw new ArgumentNullException(nameof(nativeHost));
	}

	public void Pause()
	{
		if (_mediaPlayer != IntPtr.Zero)
		{
			IMFPMediaPlayer_Pause(_mediaPlayer);
		}
	}

	public async void Play(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return;
		}

		// Get HWND from DumbWindow
		var hwnd = await GetHwndAsync();
		if (hwnd == IntPtr.Zero)
		{
			throw new Exception("Failed to retrieve NativeControlHost HWND.");
		}

		if ((_mediaPlayer != IntPtr.Zero) && string.Equals(url, _currentUrl))
		{
			IMFPMediaPlayer_Play(_mediaPlayer);
			return;
		}

		if (_mediaPlayer != IntPtr.Zero)
		{
			IMFPMediaPlayer_Stop(_mediaPlayer);
			Marshal.Release(_mediaPlayer);
			_mediaPlayer = IntPtr.Zero;
		}

		var hrCreate = MFPCreateMediaPlayer(url, false, 0, IntPtr.Zero, hwnd, out _mediaPlayer);
		if ((hrCreate != 0) || (_mediaPlayer == IntPtr.Zero))
		{
			throw new Exception($"MFPCreateMediaPlayer failed: HRESULT {hrCreate:X8}");
		}

		_currentUrl = url;
		IMFPMediaPlayer_Play(_mediaPlayer);
	}

	public void Stop()
	{
		if (_mediaPlayer != IntPtr.Zero)
		{
			IMFPMediaPlayer_Stop(_mediaPlayer);
		}
	}

	private async Task<IntPtr> GetHwndAsync()
	{
		var hostType = typeof(NativeControlHost);
		var nativeHandleField = hostType.GetField("_nativeControlHandle", BindingFlags.NonPublic | BindingFlags.Instance);
		if (nativeHandleField == null)
		{
			throw new Exception("Could not find _nativeControlHandle field.");
		}

		object dumbWindow = null;
		for (var i = 0; i < 10; i++)
		{
			dumbWindow = nativeHandleField.GetValue(_nativeHost);
			if (dumbWindow != null)
			{
				break;
			}
			await Task.Delay(100);
		}

		if (dumbWindow == null)
		{
			throw new Exception("DumbWindow is still null after waiting.");
		}

		var dumbWindowType = dumbWindow.GetType();
		var handleProp = dumbWindowType.GetProperty("Handle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		if (handleProp == null)
		{
			throw new Exception("Could not find Handle property in DumbWindow.");
		}

		var hwnd = (IntPtr) handleProp.GetValue(dumbWindow)!;
		return hwnd;
	}

	private static void IMFPMediaPlayer_Pause(IntPtr pPlayer)
	{
		((IMFPMediaPlayer) Marshal.GetObjectForIUnknown(pPlayer)).Pause();
	}

	private static void IMFPMediaPlayer_Play(IntPtr pPlayer)
	{
		((IMFPMediaPlayer) Marshal.GetObjectForIUnknown(pPlayer)).Play();
	}

	private static void IMFPMediaPlayer_Stop(IntPtr pPlayer)
	{
		((IMFPMediaPlayer) Marshal.GetObjectForIUnknown(pPlayer)).Stop();
	}

	[DllImport("mfplay.dll")]
	private static extern int MFPCreateMediaPlayer(
		[MarshalAs(UnmanagedType.LPWStr)] string pwszURL,
		[MarshalAs(UnmanagedType.Bool)] bool fStartPlayback,
		uint creationOptions,
		IntPtr pCallback,
		IntPtr hWnd,
		out IntPtr ppPlayer);

	[DllImport("mfplat.dll")]
	private static extern int MFShutdown();

	[DllImport("mfplat.dll")]
	private static extern int MFStartup(uint Version, uint dwFlags);

	#endregion

	#region Interfaces

	[ComImport]
	[Guid("A714590A-58AF-430a-85BF-44F5EC838D85")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMFPMediaPlayer
	{
		[PreserveSig]
		int Play();

		[PreserveSig]
		int Pause();

		[PreserveSig]
		int Stop();

		[PreserveSig]
		int Shutdown();

		[PreserveSig]
		int AddRef();

		[PreserveSig]
		int Release();
	}

	#endregion
}