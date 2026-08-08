#region References

using System;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Media;
using Android.Views;
using Android.Widget;
using Avalonia.Android;
using Avalonia.Controls;
using Avalonia.Platform;
using Cornerstone.Avalonia.MediaPlayer;
using Debug = System.Diagnostics.Debug;
using Object = Java.Lang.Object;
using Uri = Android.Net.Uri;
using AndroidMediaPlayer = Android.Media.MediaPlayer;

#endregion

namespace Cornerstone.Avalonia.Platforms.Android;

/// <summary>
/// In-process Android player using platform MediaPlayer + TextureView (no third-party NuGets).
/// Hosted in Avalonia via AndroidViewControlHandle, same embedding model as the camera.
/// </summary>
public class MediaPlayerAdapter : BaseMediaPlayerAdapter
{
	#region Fields

	private bool _fillMode;
	private FrameLayout _hostView;
	private bool _isMuted;
	private NativeControlHost _nativeHost;
	private string _pendingSource;
	private bool _pendingSourceIsFile;
	private AndroidMediaPlayer _player;
	private IPlatformHandle _platformHandle;
	private bool _prepared;
	private MediaPlaybackState _state = MediaPlaybackState.Stopped;
	private Surface _surface;
	private bool _surfaceReady;
	private SurfaceTextureListener _surfaceTextureListener;
	private TextureView _textureView;
	private int _videoHeight;
	private int _videoWidth;
	private double _volume = 1.0;

	#endregion

	#region Properties

	public override TimeSpan Duration
	{
		get
		{
			try
			{
				if ((_player == null) || !_prepared)
				{
					return TimeSpan.Zero;
				}

				var ms = _player.Duration;
				return ms < 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(ms);
			}
			catch
			{
				return TimeSpan.Zero;
			}
		}
	}

	public override bool IsMuted
	{
		get => _isMuted;
		set
		{
			_isMuted = value;
			ApplyVolume();
		}
	}

	/// <inheritdoc />
	public override IPlatformHandle PlatformHandle => _platformHandle;

	public override TimeSpan Position
	{
		get
		{
			try
			{
				if ((_player == null) || !_prepared)
				{
					return TimeSpan.Zero;
				}

				return TimeSpan.FromMilliseconds(_player.CurrentPosition);
			}
			catch
			{
				return TimeSpan.Zero;
			}
		}
		set
		{
			try
			{
				if ((_player == null) || !_prepared)
				{
					return;
				}

				var ms = (int) Math.Max(0, value.TotalMilliseconds);
				_player.SeekTo(ms);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"MediaPlayer seek failed: {ex.Message}");
			}
		}
	}

	public override MediaPlaybackState State => _state;

	public override double Volume
	{
		get => _volume;
		set
		{
			_volume = Math.Clamp(value, 0d, 1d);
			ApplyVolume();
		}
	}

	#endregion

	#region Methods

	public override void Initialize(NativeControlHost nativeHost)
	{
		_nativeHost = nativeHost ?? throw new ArgumentNullException(nameof(nativeHost));
		EnsureHostCreated();
		OnInitialized();
	}

	public override void Pause()
	{
		try
		{
			if ((_player != null) && _prepared && _player.IsPlaying)
			{
				_player.Pause();
				SetState(MediaPlaybackState.Paused);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"MediaPlayer pause failed: {ex.Message}");
		}
	}

	public override void Play(string uri)
	{
		if (string.IsNullOrWhiteSpace(uri))
		{
			return;
		}

		_ = PlayCoreAsync(uri, isFile: false);
	}

	public override void PlayFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return;
		}

		_ = PlayCoreAsync(filePath, isFile: true);
	}

	public override void Resume()
	{
		try
		{
			if ((_player == null) || !_prepared)
			{
				return;
			}

			if ((Duration > TimeSpan.Zero) && (Position >= Duration - TimeSpan.FromMilliseconds(250)))
			{
				Position = TimeSpan.Zero;
			}

			_player.Start();
			SetState(MediaPlaybackState.Playing);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"MediaPlayer resume failed: {ex.Message}");
		}
	}

	public override void SetVideoStretch(bool fill)
	{
		_fillMode = fill;
		UpdateTextureTransform();
	}

	public override void Stop()
	{
		try
		{
			if (_player != null)
			{
				if (_prepared)
				{
					_player.Stop();
				}

				_player.Reset();
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"MediaPlayer stop failed: {ex.Message}");
		}

		_prepared = false;
		_pendingSource = null;
		SetState(MediaPlaybackState.Stopped);
	}

	public override void UpdateVideoLayout()
	{
		UpdateTextureTransform();
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			ReleasePlayer();

			if (_textureView != null)
			{
				_textureView.SurfaceTextureListener = null;
				_textureView.Dispose();
				_textureView = null;
			}

			_surfaceTextureListener?.Dispose();
			_surfaceTextureListener = null;

			if (_hostView != null)
			{
				_hostView.Dispose();
				_hostView = null;
			}

			_platformHandle = null;
			OnClosed();
		}

		base.Dispose(disposing);
	}

	private void ApplyVolume()
	{
		if (_player == null)
		{
			return;
		}

		try
		{
			var level = _isMuted ? 0f : (float) _volume;
			_player.SetVolume(level, level);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"MediaPlayer volume failed: {ex.Message}");
		}
	}

	private void EnsureHostCreated()
	{
		if (_hostView != null)
		{
			return;
		}

		var context = (global::Android.Content.Context) AndroidHost.Activity
			?? AndroidApplication.Context;

		_hostView = new FrameLayout(context)
		{
			LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.MatchParent)
		};
		_hostView.SetBackgroundColor(Color.Black);

		_textureView = new TextureView(context)
		{
			LayoutParameters = new FrameLayout.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.MatchParent)
		};
		_surfaceTextureListener = new SurfaceTextureListener(this);
		_textureView.SurfaceTextureListener = _surfaceTextureListener;
		_hostView.AddView(_textureView);

		_platformHandle = new AndroidViewControlHandle(_hostView);
	}

	private void EnsurePlayer()
	{
		if (_player != null)
		{
			return;
		}

		_player = new AndroidMediaPlayer();
		_player.Prepared += OnPlayerPrepared;
		_player.Completion += OnPlayerCompletion;
		_player.Error += OnPlayerError;
		_player.VideoSizeChanged += OnPlayerVideoSizeChanged;

		using var audioAttributes = new AudioAttributes.Builder()
			.SetUsage(AudioUsageKind.Media)
			.SetContentType(AudioContentType.Movie)
			.Build();
		_player.SetAudioAttributes(audioAttributes);
		ApplyVolume();
	}

	private void HandleSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
	{
		_surface?.Release();
		_surface = new Surface(surface);
		_surfaceReady = true;

		try
		{
			_player?.SetSurface(_surface);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"MediaPlayer SetSurface failed: {ex.Message}");
		}

		UpdateTextureTransform();

		if (!string.IsNullOrEmpty(_pendingSource) && !_prepared)
		{
			_ = PreparePendingAsync();
		}
	}

	private bool HandleSurfaceTextureDestroyed(SurfaceTexture surface)
	{
		_surfaceReady = false;
		try
		{
			_player?.SetSurface(null);
		}
		catch
		{
			// ignore
		}

		_surface?.Release();
		_surface = null;
		return true;
	}

	private void HandleSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
	{
		UpdateTextureTransform();
	}

	private void OnPlayerCompletion(object sender, EventArgs e)
	{
		SetState(MediaPlaybackState.Paused);
		OnPlaybackEnded();
	}

	private void OnPlayerError(object sender, AndroidMediaPlayer.ErrorEventArgs e)
	{
		Debug.WriteLine($"MediaPlayer error: what={e.What}, extra={e.Extra}");
		e.Handled = true;
		SetState(MediaPlaybackState.Stopped);
	}

	private void OnPlayerPrepared(object sender, EventArgs e)
	{
		if (_player == null)
		{
			return;
		}

		_prepared = true;
		_videoWidth = _player.VideoWidth;
		_videoHeight = _player.VideoHeight;
		UpdateTextureTransform();
		OnMediaOpened();
		ApplyVolume();

		try
		{
			_player.Start();
			SetState(MediaPlaybackState.Playing);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"MediaPlayer start after prepare failed: {ex.Message}");
			SetState(MediaPlaybackState.Stopped);
		}
	}

	private void OnPlayerVideoSizeChanged(object sender, AndroidMediaPlayer.VideoSizeChangedEventArgs e)
	{
		_videoWidth = e.Width;
		_videoHeight = e.Height;
		UpdateTextureTransform();
	}

	private async Task PlayCoreAsync(string source, bool isFile)
	{
		try
		{
			EnsureHostCreated();
			EnsurePlayer();

			_pendingSource = source;
			_pendingSourceIsFile = isFile;
			_prepared = false;

			// Wait until Avalonia has parented the TextureView (same lesson as camera).
			await WaitForHostReadyAsync().ConfigureAwait(true);

			if (!_surfaceReady)
			{
				// SurfaceTexture callback will call PreparePendingAsync when ready.
				return;
			}

			await PreparePendingAsync().ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"MediaPlayer play failed: {ex.Message}");
			SetState(MediaPlaybackState.Stopped);
		}
	}

	private async Task PreparePendingAsync()
	{
		if (string.IsNullOrEmpty(_pendingSource) || (_player == null))
		{
			return;
		}

		var source = _pendingSource;
		var isFile = _pendingSourceIsFile;

		try
		{
			_player.Reset();
			_prepared = false;
			SetState(MediaPlaybackState.Stopped);

			var context = (global::Android.Content.Context) AndroidHost.Activity
				?? AndroidApplication.Context;

			if (isFile)
			{
				_player.SetDataSource(source);
			}
			else
			{
				_player.SetDataSource(context, Uri.Parse(source));
			}

			if (_surfaceReady && (_surface != null))
			{
				_player.SetSurface(_surface);
			}

			_player.PrepareAsync();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"MediaPlayer prepare failed: {ex.Message}");
			SetState(MediaPlaybackState.Stopped);
		}

		await Task.CompletedTask;
	}

	private void ReleasePlayer()
	{
		try
		{
			if (_player != null)
			{
				try
				{
					_player.Prepared -= OnPlayerPrepared;
					_player.Completion -= OnPlayerCompletion;
					_player.Error -= OnPlayerError;
					_player.VideoSizeChanged -= OnPlayerVideoSizeChanged;
				}
				catch
				{
					// ignore
				}

				try
				{
					if (_prepared)
					{
						_player.Stop();
					}
				}
				catch
				{
					// ignore
				}

				_player.Release();
				_player.Dispose();
				_player = null;
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"MediaPlayer release failed: {ex.Message}");
		}

		_prepared = false;
		_surface?.Release();
		_surface = null;
		_surfaceReady = false;
		SetState(MediaPlaybackState.Stopped);
	}

	private void SetState(MediaPlaybackState state)
	{
		if (_state == state)
		{
			return;
		}

		_state = state;
		OnStateChanged();
	}

	/// <summary>
	/// Letterbox (fit) or crop-fill via TextureView transform. Default is fit (preserve aspect).
	/// </summary>
	private void UpdateTextureTransform()
	{
		if ((_textureView == null) || (_videoWidth <= 0) || (_videoHeight <= 0))
		{
			return;
		}

		var viewWidth = _textureView.Width;
		var viewHeight = _textureView.Height;
		if ((viewWidth <= 0) || (viewHeight <= 0))
		{
			return;
		}

		float scaleX = 1f;
		float scaleY = 1f;
		var viewAspect = (float) viewWidth / viewHeight;
		var videoAspect = (float) _videoWidth / _videoHeight;

		if (_fillMode)
		{
			// Cover: crop so the view is filled.
			if (videoAspect > viewAspect)
			{
				scaleX = videoAspect / viewAspect;
			}
			else
			{
				scaleY = viewAspect / videoAspect;
			}
		}
		else
		{
			// Fit: letterbox/pillarbox — one axis wins.
			if (videoAspect > viewAspect)
			{
				scaleY = viewAspect / videoAspect;
			}
			else
			{
				scaleX = videoAspect / viewAspect;
			}
		}

		var matrix = new Matrix();
		matrix.SetScale(scaleX, scaleY, viewWidth / 2f, viewHeight / 2f);
		_textureView.SetTransform(matrix);
	}

	private async Task WaitForHostReadyAsync()
	{
		// Give Avalonia a chance to attach AndroidViewControlHandle (MediaPlayerNativeHost).
		await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(
			() => { },
			global::Avalonia.Threading.DispatcherPriority.Loaded);

		const int maxAttempts = 120;
		for (var attempt = 0; attempt < maxAttempts; attempt++)
		{
			var parented = _hostView?.Parent != null;
			var attached = _textureView?.IsAttachedToWindow ?? false;
			var width = _textureView?.Width ?? 0;
			var height = _textureView?.Height ?? 0;

			if (parented && attached && (width > 1) && (height > 1))
			{
				UpdateTextureTransform();
				return;
			}

			_textureView?.RequestLayout();
			_hostView?.RequestLayout();

			await Task.Delay(50).ConfigureAwait(true);
			await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(
				() => { },
				global::Avalonia.Threading.DispatcherPriority.Render);
		}

		Debug.WriteLine(
			$"MediaPlayer TextureView not fully hosted (parent={_hostView?.Parent != null}, " +
			$"attached={_textureView?.IsAttachedToWindow}, size={_textureView?.Width}x{_textureView?.Height}).");
	}

	#endregion

	#region Nested Types

	/// <summary>
	/// Java peer for TextureView surface callbacks. Must be Java.Lang.Object — cannot live on the adapter itself.
	/// </summary>
	private sealed class SurfaceTextureListener : Object, TextureView.ISurfaceTextureListener
	{
		private readonly MediaPlayerAdapter _owner;

		public SurfaceTextureListener(MediaPlayerAdapter owner)
		{
			_owner = owner;
		}

		public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
		{
			_owner.HandleSurfaceTextureAvailable(surface, width, height);
		}

		public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
		{
			return _owner.HandleSurfaceTextureDestroyed(surface);
		}

		public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
		{
			_owner.HandleSurfaceTextureSizeChanged(surface, width, height);
		}

		public void OnSurfaceTextureUpdated(SurfaceTexture surface)
		{
		}
	}

	#endregion
}