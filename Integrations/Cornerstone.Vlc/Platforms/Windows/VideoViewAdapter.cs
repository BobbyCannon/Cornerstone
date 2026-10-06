#region References

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.Platform;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using LibVLCSharp.Shared;
using LibVlcMediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using LibVlcMedia = LibVLCSharp.Shared.Media;
using LibVlcVideoView = LibVLCSharp.WinForms.VideoView;

#endregion

namespace Cornerstone.Vlc.Platforms.Windows;

[SourceReflection]
[SupportedOSPlatform("Windows")]
internal class VideoViewAdapter : CornerstoneObject, IVideoViewAdapter, IDisposable
{
	#region Constants

	private const int SwHide = 0;
	private const int SwShow = 5;
	private const uint SwpHideWindow = 0x0080;
	private const uint SwpNoMove = 0x0002;
	private const uint SwpNoSize = 0x0001;
	private const uint SwpNoZOrder = 0x0004;
	private const uint SwpShowWindow = 0x0040;

	#endregion

	#region Fields

	private bool _disposed;
	private readonly LibVLC _libVlc;
	private readonly LibVlcMediaPlayer _mediaPlayer;
	private readonly LibVlcVideoView _videoView;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public VideoViewAdapter()
	{
		IsNativeSurfaceVisible = true;
		Core.Initialize();
		_libVlc = new LibVLC();
		_mediaPlayer = new LibVlcMediaPlayer(_libVlc);
		_videoView = new LibVlcVideoView
		{
			MediaPlayer = _mediaPlayer
		};
		PlatformHandle = new PlatformHandle(_videoView.Handle, "HWND");
	}

	#endregion

	#region Properties

	public bool IsNativeSurfaceVisible { get; private set; }

	public LibVlcMediaPlayer MediaPlayer => _mediaPlayer;

	public IPlatformHandle PlatformHandle { get; }

	public Uri Source { get; set; }

	#endregion

	#region Methods

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_mediaPlayer.Stop();
		_videoView.MediaPlayer = null;
		_videoView.Dispose();
		_mediaPlayer.Dispose();
		_libVlc.Dispose();
	}

	public void HandleResize(int width, int height, float scaling)
	{
		if (!IsNativeSurfaceVisible)
		{
			return;
		}

		SetWindowPos(_videoView.Handle, IntPtr.Zero, 0, 0, Math.Max(0, width), Math.Max(0, height), SwpNoZOrder);
	}

	public void Pause()
	{
		_mediaPlayer.Pause();
	}

	public void Play()
	{
		if (Source == null)
		{
			return;
		}

		using var media = new LibVlcMedia(_libVlc, Source);
		_mediaPlayer.Play(media);
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
		_videoView.Visible = visible;
		var hwnd = _videoView.Handle;
		if (hwnd == IntPtr.Zero)
		{
			return;
		}

		if (visible)
		{
			ShowWindow(hwnd, SwShow);
			SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpShowWindow);
		}
		else
		{
			ShowWindow(hwnd, SwHide);
			SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoZOrder | SwpHideWindow);
		}
	}

	public void Stop()
	{
		_mediaPlayer.Stop();
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	#endregion
}
