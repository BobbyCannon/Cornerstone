#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using CoreGraphics;
using CoreMedia;
using Cornerstone.Presentation.Controls.MediaPlayer;
using Foundation;
using UIKit;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.Presentation.Platforms.iOS;

public class MediaPlayerAdapter : BaseMediaPlayerAdapter
{
	#region Fields

	private string _currentSource;
	private NSObject _endObserver;
	private Task _initializeTask = Task.CompletedTask;
	private bool _isMuted;
	private NativeControlHost _nativeHost;
	private UIView _nativeView;
	private int _playVersion;
	private AVPlayer _player;
	private AVPlayerItem _playerItem;
	private AVPlayerLayer _playerLayer;
	private double _volume = 1.0;

	#endregion

	#region Properties

	public override TimeSpan Duration
	{
		get
		{
			var duration = _playerItem?.Duration ?? CMTime.Invalid;
			return ToTimeSpan(duration);
		}
	}

	public override bool IsMuted
	{
		get => _isMuted;
		set
		{
			_isMuted = value;
			if (_player != null)
			{
				_player.Muted = value;
			}
		}
	}

	public override TimeSpan Position
	{
		get
		{
			if (_player == null)
			{
				return TimeSpan.Zero;
			}

			return ToTimeSpan(_player.CurrentTime);
		}
		set
		{
			if (_player == null)
			{
				return;
			}

			var seconds = Math.Max(0, value.TotalSeconds);
			_player.Seek(CMTime.FromSeconds(seconds, 600), CMTime.Zero, CMTime.Zero);
		}
	}

	public override MediaPlaybackState State
	{
		get
		{
			if ((_player == null) || (_playerItem == null))
			{
				return MediaPlaybackState.Stopped;
			}

			return _player.TimeControlStatus switch
			{
				AVPlayerTimeControlStatus.Playing => MediaPlaybackState.Playing,
				AVPlayerTimeControlStatus.WaitingToPlayAtSpecifiedRate => MediaPlaybackState.Playing,
				AVPlayerTimeControlStatus.Paused => MediaPlaybackState.Paused,
				_ => MediaPlaybackState.Stopped
			};
		}
	}

	public override double Volume
	{
		get => _volume;
		set
		{
			_volume = Math.Clamp(value, 0d, 1d);
			if (_player != null)
			{
				_player.Volume = (float) _volume;
			}
		}
	}

	#endregion

	#region Methods

	public override void Initialize(NativeControlHost nativeHost)
	{
		_nativeHost = nativeHost ?? throw new ArgumentNullException(nameof(nativeHost));
		_initializeTask = InitializeAsync();
	}

	public override void Pause()
	{
		_player?.Pause();
		OnStateChanged();
	}

	public override async void Play(string uri)
	{
		if (string.IsNullOrEmpty(uri))
		{
			throw new ArgumentNullException(nameof(uri));
		}

		var url = NSUrl.FromString(uri);
		if (url == null)
		{
			throw new ArgumentException("Invalid URI", nameof(uri));
		}

		await PlayInternalAsync(url, uri);
	}

	public override async void PlayFile(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			throw new ArgumentNullException(nameof(filePath));
		}

		// Prefer file URL construction so local playback paths resolve correctly on iOS.
		var url = NSUrl.FromFilename(filePath) ?? NSUrl.FromString(new Uri(filePath).AbsoluteUri);
		if (url == null)
		{
			throw new ArgumentException("Invalid file path", nameof(filePath));
		}

		await PlayInternalAsync(url, filePath);
	}

	public override void Resume()
	{
		if (_player == null)
		{
			return;
		}

		// Restart from the beginning when resuming at/after the end.
		if ((Duration > TimeSpan.Zero) && (Position >= Duration - TimeSpan.FromMilliseconds(250)))
		{
			Position = TimeSpan.Zero;
		}

		_player.Play();
		OnStateChanged();
	}

	public override void SetVideoStretch(bool fill)
	{
		if (_playerLayer == null)
		{
			return;
		}

		_playerLayer.VideoGravity = fill
			? AVLayerVideoGravity.Resize
			: AVLayerVideoGravity.ResizeAspect;
	}

	public override void Stop()
	{
		Interlocked.Increment(ref _playVersion);
		_player?.Pause();
		ClearCurrentItem();
		_currentSource = null;
		OnStateChanged();
	}

	public override void UpdateVideoLayout()
	{
		UpdatePlayerLayerFrame();
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		Interlocked.Increment(ref _playVersion);
		Stop();

		if (_nativeHost != null)
		{
			_nativeHost.PropertyChanged -= OnNativeHostOnPropertyChanged;
		}

		_playerLayer?.RemoveFromSuperLayer();
		_playerLayer?.Dispose();
		_player?.Dispose();

		_nativeView?.RemoveFromSuperview();
		_nativeView?.Dispose();

		_player = null;
		_playerLayer = null;
		_nativeView = null;
		_nativeHost = null;
		base.Dispose(disposing);
	}

	private async Task AttachNativeViewAsync()
	{
		if ((_nativeHost == null) || (_nativeView == null) || (_nativeView.Superview != null))
		{
			return;
		}

		var platformHandle = await _nativeHost.GetHwndAsync();
		if (platformHandle == IntPtr.Zero)
		{
			return;
		}

		var parentView = ObjCRuntime.Runtime.GetNSObject<UIView>(platformHandle);
		if (parentView == null)
		{
			return;
		}

		parentView.AddSubview(_nativeView);
		parentView.BringSubviewToFront(_nativeView);
		UpdatePlayerLayerFrame();
	}


	private void ClearCurrentItem()
	{
		if (_endObserver != null)
		{
			NSNotificationCenter.DefaultCenter.RemoveObserver(_endObserver);
			_endObserver.Dispose();
			_endObserver = null;
		}

		_player?.ReplaceCurrentItemWithPlayerItem(null);
		_playerItem?.Dispose();
		_playerItem = null;
	}

	private static void EnsureAudioSession()
	{
		var session = AVAudioSession.SharedInstance();
		session.SetCategory(AVAudioSessionCategory.Playback);
		session.SetActive(true);
	}

	private async Task EnsureReadyAsync()
	{
		await _initializeTask;
		await AttachNativeViewAsync();

		if ((_player == null) || (_nativeView == null))
		{
			throw new InvalidOperationException("iOS media player is not initialized.");
		}
	}

	private async Task InitializeAsync()
	{
		EnsureAudioSession();

		_nativeView = new UIView
		{
			BackgroundColor = UIColor.Black,
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
			ClipsToBounds = true
		};

		_player = new AVPlayer
		{
			Volume = (float) _volume,
			Muted = _isMuted,
			ActionAtItemEnd = AVPlayerActionAtItemEnd.Pause
		};

		_playerLayer = AVPlayerLayer.FromPlayer(_player);
		_playerLayer.VideoGravity = AVLayerVideoGravity.ResizeAspect;
		_nativeView.Layer.AddSublayer(_playerLayer);

		// Host handle may not exist yet if the control is still invisible; Play retries attach.
		await AttachNativeViewAsync();

		_nativeHost.PropertyChanged += OnNativeHostOnPropertyChanged;
		OnInitialized();
	}

	private void OnNativeHostOnPropertyChanged(object s, PresentationPropertyChangedEventArgs e)
	{
		if (e.Property.Name == nameof(NativeControlHost.Bounds))
		{
			UpdatePlayerLayerFrame();
		}
	}

	private void OnPlayToEnd(NSNotification notification)
	{
		UIApplication.SharedApplication.InvokeOnMainThread(() =>
		{
			_player?.Seek(CMTime.Zero);
			_player?.Pause();
			OnPlaybackEnded();
			OnStateChanged();
		});
	}

	private async Task PlayInternalAsync(NSUrl url, string sourceKey)
	{
		await EnsureReadyAsync();
		EnsureAudioSession();

		// Same source already loaded (e.g. finished) - restart and play.
		if (string.Equals(sourceKey, _currentSource, StringComparison.Ordinal)
			&& (_playerItem != null)
			&& (_playerItem.Status == AVPlayerItemStatus.ReadyToPlay))
		{
			Position = TimeSpan.Zero;
			_player.Play();
			OnStateChanged();
			return;
		}

		var playVersion = Interlocked.Increment(ref _playVersion);
		ClearCurrentItem();

		var asset = AVAsset.FromUrl(url);
		if (asset == null)
		{
			throw new InvalidOperationException($"Unable to open media: {sourceKey}");
		}

		_playerItem = new AVPlayerItem(asset);
		_endObserver = NSNotificationCenter.DefaultCenter.AddObserver(
			AVPlayerItem.DidPlayToEndTimeNotification,
			OnPlayToEnd,
			_playerItem
		);

		_player.ReplaceCurrentItemWithPlayerItem(_playerItem);
		_currentSource = sourceKey;

		var ready = await WaitForReadyAsync(_playerItem, TimeSpan.FromSeconds(20));
		if (playVersion != _playVersion)
		{
			return;
		}

		if (!ready || (_playerItem.Status != AVPlayerItemStatus.ReadyToPlay))
		{
			var error = _playerItem.Error?.LocalizedDescription ?? "unknown error";
			throw new InvalidOperationException($"iOS media failed to become ready: {error}");
		}

		UpdatePlayerLayerFrame();
		_player.Volume = (float) _volume;
		_player.Muted = _isMuted;

		OnMediaOpened();
		_player.Play();
		OnStateChanged();
	}

	private static TimeSpan ToTimeSpan(CMTime time)
	{
		if (time.IsInvalid || time.IsIndefinite || double.IsNaN(time.Seconds) || double.IsInfinity(time.Seconds))
		{
			return TimeSpan.Zero;
		}

		return TimeSpan.FromSeconds(Math.Max(0, time.Seconds));
	}

	private void UpdatePlayerLayerFrame()
	{
		if ((_playerLayer == null) || (_nativeView == null) || (_nativeHost == null))
		{
			return;
		}

		void Apply()
		{
			if ((_playerLayer == null) || (_nativeView == null) || (_nativeHost == null))
			{
				return;
			}

			var bounds = _nativeHost.Bounds;
			var width = Math.Max(0, bounds.Width);
			var height = Math.Max(0, bounds.Height);
			var rect = new CGRect(0, 0, width, height);

			// Match the Cornerstone host size in the parent native view coordinates.
			if (_nativeView.Superview != null)
			{
				_nativeView.Frame = rect;
			}

			_playerLayer.Frame = _nativeView.Bounds.IsEmpty ? rect : _nativeView.Bounds;
		}

		if (NSThread.IsMain)
		{
			Apply();
		}
		else
		{
			UIApplication.SharedApplication.InvokeOnMainThread(Apply);
		}
	}

	private static async Task<bool> WaitForReadyAsync(AVPlayerItem item, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;

		while (DateTime.UtcNow < deadline)
		{
			switch (item.Status)
			{
				case AVPlayerItemStatus.ReadyToPlay:
					return true;
				case AVPlayerItemStatus.Failed:
					return false;
			}

			await Task.Delay(50);
		}

		return item.Status == AVPlayerItemStatus.ReadyToPlay;
	}

	#endregion
}
