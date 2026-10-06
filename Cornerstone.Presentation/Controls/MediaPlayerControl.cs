#region References

using System;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.MediaPlayer;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.NativeHosts;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Threading;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Presentation.Controls;

[TemplatePart(PartNativeHost, typeof(MediaPlayerNativeHost))]
[TemplatePart(PartPlayBar, typeof(PlayBar))]
[TemplatePart(PartTapSurface, typeof(Border))]
public class MediaPlayerControl : TemplatedControl
{
	#region Constants

	public const string PartNativeHost = "PART_NativeHost";
	public const string PartPlayBar = "PART_PlayBar";
	public const string PartTapSurface = "PART_TapSurface";

	#endregion

	#region Fields

	public static readonly DirectProperty<MediaPlayerControl, string> MediaUrlProperty;
	public static readonly StyledProperty<bool> ShowMediaControlsProperty;
	private string _activeSource;
	private bool _activeSourceIsFile;
	private bool _adapterInitialized;
	private bool _attached;
	private readonly bool _fillMode;
	private readonly BaseMediaPlayerAdapter _mediaPlayerAdapter;
	private string _mediaUrl;
	private MediaPlayerNativeHost _nativeHost;
	private PlayBar _playBar;
	private bool _playbackPending;
	private Border _tapSurface;
	private readonly DispatcherTimer _timer;

	#endregion

	#region Constructors

	public MediaPlayerControl()
	{
		_fillMode = false;
		_attached = false;

		SizeChanged += OnControlSizeChanged;

		_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
		_timer.Tick += OnTimerTick;

		_mediaPlayerAdapter = ResolveAdapter();
		_mediaPlayerAdapter.Initialized += MediaPlayerAdapterOnInitialized;
		_mediaPlayerAdapter.Closed += MediaPlayerAdapterOnClosed;
		_mediaPlayerAdapter.MediaOpened += MediaPlayerAdapterOnMediaOpened;
		_mediaPlayerAdapter.StateChanged += MediaPlayerAdapterOnStateChanged;
		_mediaPlayerAdapter.PlaybackEnded += MediaPlayerAdapterOnPlaybackEnded;
	}

	static MediaPlayerControl()
	{
		MediaUrlProperty = PresentationProperty.RegisterDirect<MediaPlayerControl, string>(nameof(MediaUrl), o => o.MediaUrl, (o, v) => o.MediaUrl = v);
		ShowMediaControlsProperty = PresentationProperty.Register<MediaPlayerControl, bool>(nameof(ShowMediaControls), true);
	}

	#endregion

	#region Properties

	public TimeSpan Duration => _mediaPlayerAdapter?.Duration ?? TimeSpan.Zero;

	public string MediaUrl
	{
		get => _mediaUrl;
		set
		{
			var previous = _mediaUrl;
			SetAndRaise(MediaUrlProperty, ref _mediaUrl, value);
			if (!string.Equals(previous, value, StringComparison.Ordinal))
			{
				PlayVideo(value);
			}
		}
	}

	public TimeSpan PlaybackPosition
	{
		get => _mediaPlayerAdapter?.Position ?? TimeSpan.Zero;
		set
		{
			if (_mediaPlayerAdapter != null)
			{
				_mediaPlayerAdapter.Position = value;
			}
		}
	}

	public MediaPlaybackState PlaybackState => _mediaPlayerAdapter?.State ?? MediaPlaybackState.Stopped;

	public bool ShowMediaControls
	{
		get => GetValue(ShowMediaControlsProperty);
		set => SetValue(ShowMediaControlsProperty, value);
	}

	public double Volume
	{
		get => _playBar?.Volume ?? _mediaPlayerAdapter?.Volume ?? 1;
		set
		{
			var volume = Math.Clamp(value, 0, 1);
			if (_playBar != null)
			{
				_playBar.Volume = volume;
			}

			if (_mediaPlayerAdapter != null)
			{
				_mediaPlayerAdapter.Volume = volume;
			}
		}
	}

	#endregion

	#region Methods

	public void PauseVideo()
	{
		_mediaPlayerAdapter.Pause();
		UpdateTransportState();
	}

	public void PlayVideo(string url)
	{
		_activeSource = url;
		_activeSourceIsFile = false;

		// The adapter needs the NativeControlHost's window handle, which only exists once the
		// control is attached to the visual tree and Initialize has run. If a caller requests
		// playback before that (right after becoming visible), defer for now.
		if (!_adapterInitialized)
		{
			_playbackPending = true;
			return;
		}

		_playbackPending = false;
		_mediaPlayerAdapter.Play(url);
		OnPlaybackStarted();
	}

	public void PlayVideoFile(string fileLocation)
	{
		_activeSource = fileLocation;
		_activeSourceIsFile = true;

		if (!_adapterInitialized)
		{
			_playbackPending = true;
			return;
		}

		_playbackPending = false;
		_mediaPlayerAdapter.PlayFile(fileLocation);
		OnPlaybackStarted();
	}

	public void ResumeVideo()
	{
		_mediaPlayerAdapter.Resume();
		_timer.Start();
		UpdateTransportState();
	}

	public void StopVideo()
	{
		_timer.Stop();
		_mediaPlayerAdapter.Stop();
		_activeSource = null;
		ResetTransport();
	}

	public void TogglePlayPause()
	{
		OnPlayPauseClick(this, new RoutedEventArgs());
	}

	protected virtual void OnAdapterClosed()
	{
		AdapterClosed?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void OnAdapterInitialized()
	{
		AdapterInitialized?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		if (_adapterInitialized)
		{
			_mediaPlayerAdapter.Dispose();
			_adapterInitialized = false;
		}

		DetachTemplate();
		_nativeHost = e.NameScope.Find<MediaPlayerNativeHost>(PartNativeHost);
		_playBar = e.NameScope.Find<PlayBar>(PartPlayBar);

		if (_nativeHost != null)
		{
			_nativeHost.SetAdapter(_mediaPlayerAdapter);
		}

		if (_playBar != null)
		{
			_playBar.PlayPauseRequested += OnPlayPauseRequested;
			_playBar.MuteRequested += OnMuteRequested;
			_playBar.SeekCompleted += OnSeekCompleted;
			_playBar.VolumeChanged += OnVolumeChanged;
		}

		_tapSurface = e.NameScope.Find<Border>(PartTapSurface);
		if (_tapSurface != null)
		{
			_tapSurface.Tapped += TapSurfaceOnTapped;
		}

		UpdateTransportChrome();
		UpdateTransportState();
		UpdateVolumeGlyph();
		EnsureAdapterStarted();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_attached = true;
		base.OnAttachedToVisualTree(e);
		EnsureAdapterStarted();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_timer.Stop();
		_attached = false;
		base.OnDetachedFromVisualTree(e);
		if (_adapterInitialized)
		{
			_mediaPlayerAdapter.Dispose();
			_adapterInitialized = false;
		}
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == ShowMediaControlsProperty)
		{
			UpdateTransportChrome();
		}
	}

	private void DetachTemplate()
	{
		if (_playBar != null)
		{
			_playBar.PlayPauseRequested -= OnPlayPauseRequested;
			_playBar.MuteRequested -= OnMuteRequested;
			_playBar.SeekCompleted -= OnSeekCompleted;
			_playBar.VolumeChanged -= OnVolumeChanged;
			_playBar = null;
		}

		if (_tapSurface != null)
		{
			_tapSurface.Tapped -= TapSurfaceOnTapped;
			_tapSurface = null;
		}

		_nativeHost = null;
	}

	private void EnsureAdapterStarted()
	{
		if (!_attached || (_nativeHost == null) || _adapterInitialized)
		{
			return;
		}

		_mediaPlayerAdapter.Initialize(_nativeHost.NativeHost);

		// Android publishes AndroidViewControlHandle during Initialize — recreate native child.
		_nativeHost.RefreshPlatformSurface();
		_adapterInitialized = true;
		UpdateTransportState();
		UpdateVolumeGlyph();

		if (_playbackPending && !string.IsNullOrEmpty(_activeSource))
		{
			RestartOrPlay();
		}
	}

	private void MediaPlayerAdapterOnClosed(object sender, EventArgs e)
	{
		OnAdapterClosed();
	}

	private void MediaPlayerAdapterOnInitialized(object sender, EventArgs e)
	{
		OnAdapterInitialized();
	}

	private void MediaPlayerAdapterOnMediaOpened(object sender, EventArgs e)
	{
		void Apply()
		{
			if (_playBar != null)
			{
				_mediaPlayerAdapter.Volume = _playBar.Volume;
			}

			// Preserve aspect by default; fill only when toggle is on.
			_mediaPlayerAdapter.SetVideoStretch(_fillMode);

			RefreshTransportFromAdapter();
			_timer.Start();
		}

		if (Dispatcher.UIThread.CheckAccess())
		{
			Apply();
		}
		else
		{
			Dispatcher.UIThread.Post(Apply);
		}
	}

	private void MediaPlayerAdapterOnPlaybackEnded(object sender, EventArgs e)
	{
		void Apply()
		{
			RefreshTransportFromAdapter();
		}

		if (Dispatcher.UIThread.CheckAccess())
		{
			Apply();
		}
		else
		{
			Dispatcher.UIThread.Post(Apply);
		}
	}

	private void MediaPlayerAdapterOnStateChanged(object sender, EventArgs e)
	{
		void Apply()
		{
			UpdateTransportState();
		}

		if (Dispatcher.UIThread.CheckAccess())
		{
			Apply();
		}
		else
		{
			Dispatcher.UIThread.Post(Apply);
		}
	}

	private void OnControlSizeChanged(object sender, SizeChangedEventArgs e)
	{
		_mediaPlayerAdapter?.UpdateVideoLayout();
	}

	private void OnMuteRequested(object sender, EventArgs e)
	{
		_mediaPlayerAdapter.IsMuted = !_mediaPlayerAdapter.IsMuted;
		if (_playBar != null)
		{
			_playBar.IsMuted = _mediaPlayerAdapter.IsMuted;
		}
	}

	private void OnPlayPauseClick(object sender, RoutedEventArgs e)
	{
		switch (_mediaPlayerAdapter.State)
		{
			case MediaPlaybackState.Playing:
			{
				PauseVideo();
				break;
			}
			case MediaPlaybackState.Paused:
			{
				_mediaPlayerAdapter.Resume();
				_timer.Start();
				UpdateTransportState();
				break;
			}
			default:
			{
				RestartOrPlay();
				break;
			}
		}
	}

	private void OnPlayPauseRequested(object sender, EventArgs e)
	{
		OnPlayPauseClick(sender, new RoutedEventArgs());
	}

	private void OnPlaybackStarted()
	{
		// Always apply stretch policy (default false = preserve aspect ratio).
		_mediaPlayerAdapter.SetVideoStretch(_fillMode);
		_timer.Start();
		UpdateVolumeGlyph();
		UpdateTransportState();
	}

	private void OnSeekCompleted(object sender, PlayBarSeekEventArgs e)
	{
		_mediaPlayerAdapter.Position = e.Position;
	}

	private void OnTimerTick(object sender, EventArgs e)
	{
		RefreshTransportFromAdapter();
	}

	private void OnVolumeChanged(object sender, PlayBarVolumeEventArgs e)
	{
		_mediaPlayerAdapter.Volume = e.Volume;
		if (_mediaPlayerAdapter.IsMuted && (e.Volume > 0))
		{
			_mediaPlayerAdapter.IsMuted = false;
		}

		if (_playBar != null)
		{
			_playBar.IsMuted = _mediaPlayerAdapter.IsMuted;
		}
	}

	private void RefreshTransportFromAdapter()
	{
		if (_playBar != null)
		{
			_playBar.Duration = _mediaPlayerAdapter.Duration;
			_playBar.Position = _mediaPlayerAdapter.Position;
			_playBar.Volume = _mediaPlayerAdapter.Volume;
			_playBar.IsMuted = _mediaPlayerAdapter.IsMuted;
		}

		UpdateTransportState();
		TransportChanged?.Invoke(this, EventArgs.Empty);
	}

	private void ResetTransport()
	{
		if (_playBar != null)
		{
			_playBar.Duration = TimeSpan.Zero;
			_playBar.Position = TimeSpan.Zero;
		}

		UpdateTransportState();
	}

	private static BaseMediaPlayerAdapter ResolveAdapter()
	{
		if (Design.IsDesignMode)
		{
			return new MediaPlayerAdapterStub();
		}

		try
		{
			return AppBootstrap.GetInstance<BaseMediaPlayerAdapter>() ?? new MediaPlayerAdapterStub();
		}
		catch
		{
			return new MediaPlayerAdapterStub();
		}
	}

	private void RestartOrPlay()
	{
		if (string.IsNullOrEmpty(_activeSource))
		{
			return;
		}

		if (_activeSourceIsFile)
		{
			PlayVideoFile(_activeSource);
		}
		else
		{
			PlayVideo(_activeSource);
		}
	}

	private void TapSurfaceOnTapped(object sender, TappedEventArgs e)
	{
		if ((_playBar == null) || !ShowMediaControls || (e.Pointer.Type != PointerType.Touch))
		{
			return;
		}

		// Mouse shows and hides by hover. Touch has no hover, so a tap pins the bar open and the next tap clears it.
		_playBar.TogglePinnedChrome();
		e.Handled = true;
	}

	private void UpdateTransportChrome()
	{
		if (_playBar == null)
		{
			return;
		}

		_playBar.IsVisible = ShowMediaControls;
		if (!ShowMediaControls)
		{
			_playBar.KeepChromeOpen(false);
		}
	}

	private void UpdateTransportState()
	{
		if (_playBar == null)
		{
			return;
		}

		var playing = (_mediaPlayerAdapter != null)
			&& (_mediaPlayerAdapter.State == MediaPlaybackState.Playing);
		_playBar.IsPlaying = playing;
		UpdateTransportChrome();
	}

	private void UpdateVolumeGlyph()
	{
		if ((_playBar == null) || (_mediaPlayerAdapter == null))
		{
			return;
		}

		_playBar.IsMuted = _mediaPlayerAdapter.IsMuted;
		_playBar.Volume = _mediaPlayerAdapter.Volume;
	}

	#endregion

	#region Events

	public event EventHandler AdapterClosed;
	public event EventHandler AdapterInitialized;
	public event EventHandler TransportChanged;

	#endregion
}