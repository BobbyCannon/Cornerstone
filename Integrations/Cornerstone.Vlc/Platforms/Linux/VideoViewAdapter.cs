#region References

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platforms.Desktop;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using LibVLCSharp.Shared;
using LibVlcMediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using LibVlcMedia = LibVLCSharp.Shared.Media;

#endregion

namespace Cornerstone.Vlc.Platforms.Linux;

/// <summary>
/// LibVLC on Linux. X11 embeds in the child window. Wayland copies RV32 frames
/// into the Cornerstone subsurface (a second wl_display cannot parent here).
/// </summary>
[SourceReflection]
internal sealed class VideoViewAdapter : CornerstoneObject, IVideoViewAdapter, IDisposable
{
	#region Fields

	private bool _callbacksSet;
	private bool _disposed;
	private IntPtr _frame;
	private int _frameBytes;
	private int _frameHeight;
	private int _frameWidth;
	private readonly object _frameGate;
	private int _hostHeight;
	private int _hostWidth;
	private int _offsetX;
	private int _offsetY;
	private int _surfaceHeight;
	private int _surfaceWidth;
	private LibVLC _libVlc;
	private LibVlcMediaPlayer _mediaPlayer;
	private string _missingLibrary;
	private INativePixelBufferHost _pixels;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public VideoViewAdapter()
	{
		IsNativeSurfaceVisible = true;
		PlatformHandle = null;
		_frameGate = new object();
		_offsetX = 0;
		_offsetY = 0;
		_surfaceWidth = 0;
		_surfaceHeight = 0;
		_hostWidth = 1280;
		_hostHeight = 720;
		try
		{
			LinuxVideoFrames.PreloadFfmpeg();
			var pluginDirectory = FindPluginDirectory();
			if (!string.IsNullOrEmpty(pluginDirectory))
			{
				Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", pluginDirectory);
			}

			Core.Initialize();
			_libVlc = string.IsNullOrEmpty(pluginDirectory)
				? new LibVLC("--no-video-title-show")
				: new LibVLC("--no-video-title-show", "--plugin-path=" + pluginDirectory);
			_mediaPlayer = new LibVlcMediaPlayer(_libVlc);
		}
		catch (Exception ex)
		{
			_missingLibrary = ex.Message;
		}
	}

	#endregion

	#region Properties

	public bool IsNativeSurfaceVisible { get; private set; }

	public LibVlcMediaPlayer MediaPlayer => _mediaPlayer;

	public IPlatformHandle PlatformHandle { get; private set; }

	public Uri Source { get; set; }

	#endregion

	#region Methods

	public IPlatformHandle AttachToHost(IPlatformHandle parent, InputElement inputHost)
	{
		if (parent is INativePixelBufferHost pixels)
		{
			_pixels = pixels;
			PlatformHandle = parent;
			return parent;
		}

		if ((parent != null) && (parent.HandleDescriptor == "XID") && (_mediaPlayer != null))
		{
			_mediaPlayer.XWindow = (uint) parent.Handle.ToInt64();
			PlatformHandle = parent;
			return parent;
		}

		return parent;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		if (_mediaPlayer != null)
		{
			_mediaPlayer.Stop();
			_mediaPlayer.Dispose();
			_mediaPlayer = null;
		}

		if (_libVlc != null)
		{
			_libVlc.Dispose();
			_libVlc = null;
		}

		lock (_frameGate)
		{
			if (_frame != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_frame);
				_frame = IntPtr.Zero;
			}
		}
	}

	public void HandleResize(int width, int height, float scaling)
	{
		if ((width >= 2) && (height >= 2))
		{
			_hostWidth = width;
			_hostHeight = height;
		}
	}

	public void Pause()
	{
		_mediaPlayer?.SetPause(true);
	}

	public void Play()
	{
		if (_missingLibrary != null)
		{
			throw new InvalidOperationException(_missingLibrary);
		}

		if ((Source == null) || (_mediaPlayer == null))
		{
			return;
		}

		var missingPlugin = LinuxVideoFrames.MissingH264Plugin();
		if (missingPlugin != null)
		{
			throw new InvalidOperationException(missingPlugin);
		}

		using var media = new LibVlcMedia(_libVlc, Source);
		var parseOptions = Source.IsFile ? MediaParseOptions.ParseLocal : MediaParseOptions.ParseNetwork;
		media.Parse(parseOptions, 10000, CancellationToken.None).GetAwaiter().GetResult();
		var videoWidth = 0;
		var videoHeight = 0;
		var sarNum = 1;
		var sarDen = 1;
		foreach (var track in media.Tracks)
		{
			if (track.TrackType != TrackType.Video)
			{
				continue;
			}

			videoWidth = (int) track.Data.Video.Width;
			videoHeight = (int) track.Data.Video.Height;
			sarNum = (int) track.Data.Video.SarNum;
			sarDen = (int) track.Data.Video.SarDen;
			break;
		}

		EnsureCallbacks(videoWidth, videoHeight, sarNum, sarDen);
		_mediaPlayer.Play(media);
	}

	public void SetNativeSurfaceVisible(bool visible)
	{
		IsNativeSurfaceVisible = visible;
		if (!visible)
		{
			_mediaPlayer?.SetPause(true);
		}
	}

	public void Stop()
	{
		_mediaPlayer?.Stop();
	}

	private void DisplayVideo(IntPtr opaque, IntPtr picture)
	{
		byte[] copy;
		int width;
		int height;
		lock (_frameGate)
		{
			if ((_frame == IntPtr.Zero) || (_frameBytes <= 0))
			{
				return;
			}

			copy = new byte[_frameBytes];
			Marshal.Copy(_frame, copy, 0, _frameBytes);
			// RV32 leaves alpha at 0. Wayland ARGB8888 is premultiplied, so the hole shows the desktop.
			for (var i = 3; i < copy.Length; i += 4)
			{
				copy[i] = 0xFF;
			}

			width = _frameWidth;
			height = _frameHeight;
			copy = LinuxVideoFrames.Letterbox(copy, width, height, _surfaceWidth, _surfaceHeight, _offsetX, _offsetY);
			width = _surfaceWidth;
			height = _surfaceHeight;
		}

		var pixels = _pixels;
		if (pixels == null)
		{
			return;
		}

		// AttachBgra posts through the compositor, which requires the UI thread.
		Dispatcher.UIThread.Post(() => pixels.AttachBgra(copy, width, height));
	}

	private static string FindPluginDirectory()
	{
		var candidates = new[]
		{
			"/usr/lib/x86_64-linux-gnu/vlc/plugins",
			"/usr/lib/aarch64-linux-gnu/vlc/plugins",
			"/usr/lib64/vlc/plugins",
			"/usr/lib/vlc/plugins"
		};
		foreach (var candidate in candidates)
		{
			if (Directory.Exists(candidate))
			{
				return candidate;
			}
		}

		return null;
	}

	private void EnsureCallbacks(int videoWidth, int videoHeight, int sarNum, int sarDen)
	{
		if ((_pixels == null) || (_mediaPlayer == null))
		{
			return;
		}

		var hostWidth = _hostWidth & ~1;
		var hostHeight = _hostHeight & ~1;
		if (hostWidth < 2)
		{
			hostWidth = 1280;
		}

		if (hostHeight < 2)
		{
			hostHeight = 720;
		}

		var width = hostWidth;
		var height = hostHeight;
		var offsetX = 0;
		var offsetY = 0;
		if ((videoWidth >= 2) && (videoHeight >= 2))
		{
			LinuxVideoFrames.Fit(videoWidth, videoHeight, sarNum, sarDen, hostWidth, hostHeight, out width, out height, out offsetX, out offsetY);
		}

		_surfaceWidth = hostWidth;
		_surfaceHeight = hostHeight;
		_offsetX = offsetX;
		_offsetY = offsetY;
		var bytes = width * height * 4;
		lock (_frameGate)
		{
			if (_frame != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_frame);
			}

			_frame = Marshal.AllocHGlobal(bytes);
			_frameBytes = bytes;
			_frameWidth = width;
			_frameHeight = height;
		}

		// Fixed RV32. The format callback reports zero pictures on this host and LibVLC then drops video.
		if (!_callbacksSet)
		{
			_mediaPlayer.SetVideoCallbacks(LockVideo, null, DisplayVideo);
			_callbacksSet = true;
		}

		_mediaPlayer.SetVideoFormat("RV32", (uint) width, (uint) height, (uint) (width * 4));
	}

	private IntPtr LockVideo(IntPtr opaque, IntPtr planes)
	{
		lock (_frameGate)
		{
			// vmem reads every plane pointer. Only plane 0 is used; the rest are uninitialized.
			for (var i = 0; i < 5; i++)
			{
				Marshal.WriteIntPtr(planes, i * IntPtr.Size, IntPtr.Zero);
			}

			Marshal.WriteIntPtr(planes, _frame);
			return _frame;
		}
	}

	#endregion
}
