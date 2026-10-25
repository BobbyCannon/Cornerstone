using System;
using System.Runtime.InteropServices;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.MediaPlayer;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

namespace Cornerstone.Presentation.Platforms.Desktop;

/// <summary>
/// Linux playback through system LibVLC. X11 draws into the child X window.
/// Wayland has no embeddable foreign surface, so frames are copied into the subsurface.
/// </summary>
[SourceReflection]
internal sealed class LinuxMediaPlayerAdapter : BaseMediaPlayerAdapter
{
	#region Fields

	private readonly LinuxLibVlc.EventCallback _onEndReached;
	private readonly LinuxLibVlc.EventCallback _onPlaying;
	private readonly LinuxLibVlc.VideoDisplay _displayVideo;
	private readonly object _frameGate;
	private readonly LinuxLibVlc.VideoLock _lockVideo;
	private bool _callbacksSet;
	private IntPtr _frame;
	private int _frameBytes;
	private int _frameHeight;
	private int _frameWidth;
	private int _hostHeight;
	private int _hostWidth;
	private IntPtr _instance;
	private int _offsetX;
	private int _offsetY;
	private int _surfaceHeight;
	private int _surfaceWidth;
	private bool _memoryOutput;
	private string _missingLibrary;
	private INativePixelBufferHost _pixels;
	private IntPtr _player;
	private uint _xid;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public LinuxMediaPlayerAdapter()
	{
		_frameGate = new object();
		_offsetX = 0;
		_offsetY = 0;
		_surfaceWidth = 0;
		_surfaceHeight = 0;
		_hostWidth = 1280;
		_hostHeight = 720;
		_onPlaying = OnPlaying;
		_onEndReached = OnEndReached;
		_lockVideo = LockVideo;
		_displayVideo = DisplayVideo;
		if (!LinuxLibVlc.TryLoad(out var error))
		{
			_missingLibrary = error;
		}
	}

	#endregion

	#region Properties

	public override TimeSpan Duration
	{
		get
		{
			if (_player == IntPtr.Zero)
			{
				return TimeSpan.Zero;
			}

			var length = LinuxLibVlc.GetLength(_player);
			return length > 0 ? TimeSpan.FromMilliseconds(length) : TimeSpan.Zero;
		}
	}

	public override bool IsMuted
	{
		get => (_player != IntPtr.Zero) && LinuxLibVlc.GetMute(_player);
		set
		{
			if (_player != IntPtr.Zero)
			{
				LinuxLibVlc.SetMute(_player, value);
			}
		}
	}

	public override TimeSpan Position
	{
		get
		{
			if (_player == IntPtr.Zero)
			{
				return TimeSpan.Zero;
			}

			var time = LinuxLibVlc.GetTime(_player);
			return time > 0 ? TimeSpan.FromMilliseconds(time) : TimeSpan.Zero;
		}
		set
		{
			if (_player != IntPtr.Zero)
			{
				LinuxLibVlc.SetTime(_player, (long) value.TotalMilliseconds);
			}
		}
	}

	public override MediaPlaybackState State
	{
		get
		{
			if (_player == IntPtr.Zero)
			{
				return MediaPlaybackState.Stopped;
			}

			return LinuxLibVlc.GetState(_player) switch
			{
				LinuxLibVlc.StatePlaying => MediaPlaybackState.Playing,
				LinuxLibVlc.StatePaused => MediaPlaybackState.Paused,
				_ => MediaPlaybackState.Stopped
			};
		}
	}

	public override double Volume
	{
		get
		{
			if (_player == IntPtr.Zero)
			{
				return 1d;
			}

			var volume = LinuxLibVlc.GetVolume(_player);
			return volume < 0 ? 1d : Math.Clamp(volume / 100d, 0d, 1d);
		}
		set
		{
			if (_player != IntPtr.Zero)
			{
				LinuxLibVlc.SetVolume(_player, (int) Math.Clamp(value * 100d, 0d, 100d));
			}
		}
	}

	#endregion

	#region Methods

	public override void BindHostHandle(IPlatformHandle parent)
	{
		if (parent is INativePixelBufferHost pixels)
		{
			_pixels = pixels;
			_memoryOutput = true;
			return;
		}

		if ((parent != null) && (parent.HandleDescriptor == "XID") && (parent.Handle != IntPtr.Zero))
		{
			_xid = (uint) parent.Handle.ToInt64();
		}
	}

	public override void HandleResize(int width, int height, float scaling)
	{
		if ((width >= 2) && (height >= 2))
		{
			_hostWidth = width;
			_hostHeight = height;
		}

		base.HandleResize(width, height, scaling);
	}

	public override void Initialize(NativeControlHost nativeHost)
	{
		if (nativeHost == null)
		{
			throw new ArgumentNullException(nameof(nativeHost));
		}

		base.Initialize(nativeHost);
	}

	public override void Pause()
	{
		if (_player != IntPtr.Zero)
		{
			LinuxLibVlc.SetPause(_player, true);
			OnStateChanged();
		}
	}

	public override void Play(string uri)
	{
		if (string.IsNullOrEmpty(uri))
		{
			return;
		}

		var missingPlugin = LinuxVideoFrames.MissingH264Plugin();
		if (missingPlugin != null)
		{
			throw new InvalidOperationException(missingPlugin);
		}

		PlayCore(uri);
	}

	private void PlayCore(string uri)
	{
		EnsurePlayer();
		var media = uri.Contains("://", StringComparison.Ordinal)
			? LinuxLibVlc.CreateMediaLocation(_instance, uri)
			: LinuxLibVlc.CreateMediaPath(_instance, uri);
		if (media == IntPtr.Zero)
		{
			throw new InvalidOperationException("LibVLC could not open " + uri);
		}

		try
		{
			if (_memoryOutput)
			{
				var videoWidth = 0;
				var videoHeight = 0;
				var sarNum = 1;
				var sarDen = 1;
				if (LinuxLibVlc.TryGetVideoSize(media, out var width, out var height, out var parsedSarNum, out var parsedSarDen))
				{
					videoWidth = width;
					videoHeight = height;
					sarNum = parsedSarNum;
					sarDen = parsedSarDen;
				}

				ArmMemoryOutput(videoWidth, videoHeight, sarNum, sarDen);
			}

			LinuxLibVlc.SetMedia(_player, media);
		}
		finally
		{
			LinuxLibVlc.ReleaseMedia(media);
		}

		if (LinuxLibVlc.Play(_player) != 0)
		{
			throw new InvalidOperationException("LibVLC could not play " + uri);
		}

		OnStateChanged();
	}

	public override void PlayFile(string filePath)
	{
		Play(filePath);
	}

	public override void Resume()
	{
		if (_player != IntPtr.Zero)
		{
			LinuxLibVlc.SetPause(_player, false);
			OnStateChanged();
		}
	}

	public override void Stop()
	{
		if (_player != IntPtr.Zero)
		{
			LinuxLibVlc.Stop(_player);
			OnStateChanged();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (_player != IntPtr.Zero)
		{
			LinuxLibVlc.Stop(_player);
			LinuxLibVlc.ReleasePlayer(_player);
			_player = IntPtr.Zero;
		}

		if (_instance != IntPtr.Zero)
		{
			LinuxLibVlc.ReleaseInstance(_instance);
			_instance = IntPtr.Zero;
		}

		lock (_frameGate)
		{
			if (_frame != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(_frame);
				_frame = IntPtr.Zero;
			}
		}

		base.Dispose(disposing);
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

	private void EnsurePlayer()
	{
		if (_player != IntPtr.Zero)
		{
			return;
		}

		if (_missingLibrary != null)
		{
			throw new InvalidOperationException(_missingLibrary);
		}

		_instance = LinuxLibVlc.CreateInstance();
		if (_instance == IntPtr.Zero)
		{
			throw new InvalidOperationException(
				"libvlc_new failed. Install the vlc package so libvlc and its plugins are present. " +
				"On SteamOS: sudo steamos-readonly disable, then sudo pacman -S vlc, then sudo steamos-readonly enable.");
		}

		_player = LinuxLibVlc.CreatePlayer(_instance);
		if (_player == IntPtr.Zero)
		{
			throw new InvalidOperationException("libvlc_media_player_new failed.");
		}

		LinuxLibVlc.AttachEvent(_player, LinuxLibVlc.EventPlaying, _onPlaying);
		LinuxLibVlc.AttachEvent(_player, LinuxLibVlc.EventEndReached, _onEndReached);
		if (!_memoryOutput && (_xid != 0))
		{
			LinuxLibVlc.SetXWindow(_player, _xid);
		}
	}

	private void ArmMemoryOutput(int videoWidth, int videoHeight, int sarNum, int sarDen)
	{
		if (_player == IntPtr.Zero)
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
			LinuxLibVlc.SetVideoCallbacks(_player, _lockVideo, _displayVideo);
			_callbacksSet = true;
		}

		LinuxLibVlc.SetVideoFormat(_player, "RV32", (uint) width, (uint) height, (uint) (width * 4));
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

	private void OnEndReached(IntPtr eventHandle, IntPtr userData)
	{
		Dispatcher.UIThread.Post(() =>
		{
			OnPlaybackEnded();
			OnStateChanged();
		});
	}

	private void OnPlaying(IntPtr eventHandle, IntPtr userData)
	{
		Dispatcher.UIThread.Post(() =>
		{
			OnMediaOpened();
			OnStateChanged();
		});
	}

	#endregion
}
