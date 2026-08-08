#region References

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using Avalonia;
using Avalonia.Controls;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Avalonia.MediaPlayer;
using Foundation;
using UIKit;

#endregion

namespace Cornerstone.Avalonia.Platforms.iOS;

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
	private AVPlayerItemVideoOutput _videoOutput;
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

	/// <inheritdoc />
	public override async Task<NativeSurfaceSnapshot> CaptureSnapshotAsync(NativeSurfaceSnapshotOptions options = null)
	{
		try
		{
			// Prefer the frame currently in the playback pipeline (matches on-screen content).
			// UIView hierarchy snapshots of AVPlayerLayer are almost always black and must not
			// be treated as a successful underlay.
			var fromOutput = CaptureFrameFromVideoOutput(options);
			if (fromOutput is { Success: true })
			{
				return fromOutput;
			}

			var fromAsset = await CaptureFrameFromAssetAsync(options).ConfigureAwait(true);
			if (fromAsset is { Success: true })
			{
				return fromAsset;
			}

			Debug.WriteLine($"iOS media snapshot failed. output={fromOutput?.Error}; asset={fromAsset?.Error}");
			return fromOutput ?? fromAsset
				?? NativeSurfaceSnapshot.Failed("iOS media player snapshot failed.");
		}
		catch (Exception ex)
		{
			return NativeSurfaceSnapshot.Failed(ex.Message);
		}
	}

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

		DetachVideoOutput();
		_videoOutput?.Dispose();
		_videoOutput = null;

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

	private void AttachVideoOutput(AVPlayerItem item)
	{
		if (item == null)
		{
			return;
		}

		EnsureVideoOutput();
		DetachVideoOutput();
		item.AddOutput(_videoOutput);
	}

	private NativeSurfaceSnapshot CaptureFrameFromVideoOutput(NativeSurfaceSnapshotOptions options)
	{
		if ((_videoOutput == null) || (_player == null) || (_playerItem == null))
		{
			return NativeSurfaceSnapshot.Failed("Video output is not available.");
		}

		// Pull the sample nearest the playhead. AVPlayerItemVideoOutput keeps the decoded
		// frame that feeds AVPlayerLayer — unlike UIView snapshots, which cannot see video.
		var time = _player.CurrentTime;
		if (time.IsInvalid || time.IsIndefinite || (time.Value < 0))
		{
			time = CMTime.Zero;
		}

		CVPixelBuffer buffer = null;
		try
		{
			CMTime displayTime = default;
			if (_videoOutput.HasNewPixelBufferForItemTime(time))
			{
				buffer = _videoOutput.CopyPixelBuffer(time, ref displayTime);
			}

			// Still try even when HasNew is false (common right after pause / first frame).
			buffer ??= _videoOutput.CopyPixelBuffer(time, ref displayTime);

			if (buffer == null)
			{
				return NativeSurfaceSnapshot.Failed("No pixel buffer available from AVPlayerItemVideoOutput.");
			}

			return EncodePixelBufferToSnapshot(buffer, options);
		}
		finally
		{
			buffer?.Dispose();
		}
	}

	private async Task<NativeSurfaceSnapshot> CaptureFrameFromAssetAsync(NativeSurfaceSnapshotOptions options)
	{
		var asset = _playerItem?.Asset;
		if (asset == null)
		{
			return NativeSurfaceSnapshot.Failed("No media item loaded.");
		}

		// Ensure tracks are ready — image generation fails silently / empty when tracks are not loaded.
		var tracksReady = await WaitForAssetTracksAsync(asset, TimeSpan.FromSeconds(2)).ConfigureAwait(true);
		if (!tracksReady)
		{
			return NativeSurfaceSnapshot.Failed("Media asset tracks are not ready for image generation.");
		}

		var generator = new AVAssetImageGenerator(asset)
		{
			AppliesPreferredTrackTransform = true,
			// Zero tolerance often fails on compressed keyframe media; allow nearest frame.
			RequestedTimeToleranceAfter = CMTime.PositiveInfinity,
			RequestedTimeToleranceBefore = CMTime.PositiveInfinity,
			MaximumSize = new CGSize(1920, 1920)
		};

		var time = ResolveCaptureTime(asset);
		var tcs = new TaskCompletionSource<NativeSurfaceSnapshot>();
		generator.GenerateCGImagesAsynchronously([NSValue.FromCMTime(time)],
			(requestedTime, imageRef, actualTime, result, error) =>
			{
				try
				{
					if ((result != AVAssetImageGeneratorResult.Succeeded) || (imageRef == null))
					{
						tcs.TrySetResult(NativeSurfaceSnapshot.Failed(
							error?.LocalizedDescription ?? $"AVAssetImageGenerator result: {result}."));
						return;
					}

					// Retain pixels before the generator releases the CGImage.
					using var uiImage = new UIImage(imageRef);
					tcs.TrySetResult(EncodeUiImageToSnapshot(uiImage, options));
				}
				catch (Exception ex)
				{
					tcs.TrySetResult(NativeSurfaceSnapshot.Failed(ex.Message));
				}
				finally
				{
					generator.Dispose();
				}
			});

		return await tcs.Task.ConfigureAwait(true);
	}

	private static Task<bool> WaitForAssetTracksAsync(AVAsset asset, TimeSpan timeout)
	{
		var tcs = new TaskCompletionSource<bool>();
		asset.LoadValuesAsynchronously(new[] { "tracks", "duration", "playable" }, () =>
		{
			try
			{
				var status = asset.StatusOfValue("tracks", out var error);
				tcs.TrySetResult((status == AVKeyValueStatus.Loaded) && (error == null));
			}
			catch
			{
				tcs.TrySetResult(false);
			}
		});

		return Task.WhenAny(tcs.Task, Task.Delay(timeout))
			.ContinueWith(t => tcs.Task.IsCompletedSuccessfully && tcs.Task.Result);
	}

	private void DetachVideoOutput()
	{
		if ((_videoOutput == null) || (_playerItem == null))
		{
			return;
		}

		if (_playerItem.Outputs != null)
		{
			foreach (var output in _playerItem.Outputs)
			{
				if (ReferenceEquals(output, _videoOutput))
				{
					_playerItem.RemoveOutput(_videoOutput);
					break;
				}
			}
		}
	}

	private static NativeSurfaceSnapshot EncodePixelBufferToSnapshot(CVPixelBuffer buffer, NativeSurfaceSnapshotOptions options)
	{
		if (buffer == null)
		{
			return NativeSurfaceSnapshot.Failed("Pixel buffer was null.");
		}

		using var ciImage = new CIImage(buffer);
		using var context = new CIContext(null as CIContextOptions);
		var extent = ciImage.Extent;
		if ((extent.Width < 1) || (extent.Height < 1))
		{
			return NativeSurfaceSnapshot.Failed("Pixel buffer had empty extent.");
		}

		using var cgImage = context.CreateCGImage(ciImage, extent);
		if (cgImage == null)
		{
			return NativeSurfaceSnapshot.Failed("Failed to create CGImage from pixel buffer.");
		}

		using var uiImage = new UIImage(cgImage);
		return EncodeUiImageToSnapshot(uiImage, options);
	}

	private static NativeSurfaceSnapshot EncodeUiImageToSnapshot(UIImage uiImage, NativeSurfaceSnapshotOptions options)
	{
		if (uiImage == null)
		{
			return NativeSurfaceSnapshot.Failed("Snapshot image was null.");
		}

		using var pngData = uiImage.AsPNG();
		if ((pngData == null) || (pngData.Length == 0))
		{
			return NativeSurfaceSnapshot.Failed("Failed to encode media player snapshot as PNG.");
		}

		var bytes = pngData.ToArray();
		var scale = uiImage.CurrentScale > 0 ? uiImage.CurrentScale : 1;
		var width = (int) Math.Max(1, Math.Round(uiImage.Size.Width * scale));
		var height = (int) Math.Max(1, Math.Round(uiImage.Size.Height * scale));
		return NativeSurfaceSnapshotHelper.ProcessPng(bytes, width, height, options);
	}

	private void EnsureVideoOutput()
	{
		if (_videoOutput != null)
		{
			return;
		}

		// BGRA matches typical UIKit / CIImage conversion paths.
		var attributes = new CVPixelBufferAttributes
		{
			PixelFormatType = CVPixelFormatType.CV32BGRA
		};
		_videoOutput = new AVPlayerItemVideoOutput(attributes);
	}

	private CMTime ResolveCaptureTime(AVAsset asset)
	{
		var time = _player?.CurrentTime ?? CMTime.Zero;
		if (time.IsInvalid || time.IsIndefinite || (time.Value < 0))
		{
			time = CMTime.Zero;
		}

		var duration = asset?.Duration ?? CMTime.Invalid;
		if (!duration.IsInvalid && !duration.IsIndefinite && (duration.Seconds > 0)
			&& (time.Seconds >= duration.Seconds - 0.05))
		{
			time = CMTime.FromSeconds(Math.Max(0, duration.Seconds - 0.1), 600);
		}

		return time;
	}

	private void ClearCurrentItem()
	{
		if (_endObserver != null)
		{
			NSNotificationCenter.DefaultCenter.RemoveObserver(_endObserver);
			_endObserver.Dispose();
			_endObserver = null;
		}

		DetachVideoOutput();
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

	private void OnNativeHostOnPropertyChanged(object s, AvaloniaPropertyChangedEventArgs e)
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
		AttachVideoOutput(_playerItem);
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

			// Match the Avalonia host size in the parent native view coordinates.
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