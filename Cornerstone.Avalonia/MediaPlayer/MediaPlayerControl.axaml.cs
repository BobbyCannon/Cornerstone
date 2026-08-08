#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.MediaPlayer;

public partial class MediaPlayerControl : CornerstoneUserControl, INativeHostPausable
{
	#region Fields

	public static readonly DirectProperty<MediaPlayerControl, string> MediaUrlProperty;
	public static readonly StyledProperty<bool> IsPausedProperty;
	public static readonly StyledProperty<bool> ShowTransportControlsProperty;
	private string _activeSource;
	private bool _activeSourceIsFile;
	private bool _adapterInitialized;
	private readonly Button _aspectButton;
	private readonly TextBlock _durationText;
	private bool _fillMode;
	private bool _isSeeking;
	private readonly BaseMediaPlayerAdapter _mediaPlayerAdapter;
	private string _mediaUrl;
	private readonly Button _muteButton;
	private readonly Path _muteIcon;
	private readonly MediaPlayerNativeHost _nativeHost;
	private readonly Path _pauseIcon;
	private readonly Path _playIcon;
	private readonly Button _playPauseButton;
	private bool _playbackPending;
	private readonly TextBlock _positionText;
	private readonly Slider _seekSlider;
	private bool _suppressSeekUpdate;
	private bool _suppressVolumeUpdate;
	private readonly DispatcherTimer _timer;
	private readonly Border _transportBar;
	private readonly Path _volumeIcon;
	private readonly Slider _volumeSlider;

	#endregion

	#region Constructors

	public MediaPlayerControl()
	{
		InitializeComponent();

		_nativeHost = this.FindControl<MediaPlayerNativeHost>("NativeHost");
		_seekSlider = this.FindControl<Slider>("SeekSlider");
		_volumeSlider = this.FindControl<Slider>("VolumeSlider");
		_positionText = this.FindControl<TextBlock>("PositionText");
		_durationText = this.FindControl<TextBlock>("DurationText");
		_playPauseButton = this.FindControl<Button>("PlayPauseButton");
		_playIcon = this.FindControl<Path>("PlayIcon");
		_pauseIcon = this.FindControl<Path>("PauseIcon");
		_muteButton = this.FindControl<Button>("MuteButton");
		_volumeIcon = this.FindControl<Path>("VolumeIcon");
		_muteIcon = this.FindControl<Path>("MuteIcon");
		_aspectButton = this.FindControl<Button>("AspectButton");
		_transportBar = this.FindControl<Border>("TransportBar");
		_transportBar.IsVisible = ShowTransportControls;

		_playPauseButton.Click += OnPlayPauseClick;
		_muteButton.Click += OnMuteClick;
		_aspectButton.Click += OnAspectClick;

		_seekSlider.AddHandler(PointerPressedEvent, OnSeekPointerPressed, RoutingStrategies.Tunnel);
		_seekSlider.AddHandler(PointerReleasedEvent, OnSeekPointerReleased, RoutingStrategies.Tunnel);
		_seekSlider.ValueChanged += OnSeekValueChanged;
		_volumeSlider.ValueChanged += OnVolumeValueChanged;

		SizeChanged += OnControlSizeChanged;

		_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
		_timer.Tick += OnTimerTick;

		_mediaPlayerAdapter = GetInstance<BaseMediaPlayerAdapter>();
		_nativeHost.SetAdapter(_mediaPlayerAdapter);
		_mediaPlayerAdapter.Initialized += MediaPlayerAdapterOnInitialized;
		_mediaPlayerAdapter.Closed += MediaPlayerAdapterOnClosed;
		_mediaPlayerAdapter.MediaOpened += MediaPlayerAdapterOnMediaOpened;
		_mediaPlayerAdapter.StateChanged += MediaPlayerAdapterOnStateChanged;
		_mediaPlayerAdapter.PlaybackEnded += MediaPlayerAdapterOnPlaybackEnded;
	}

	static MediaPlayerControl()
	{
		MediaUrlProperty = AvaloniaProperty.RegisterDirect<MediaPlayerControl, string>(nameof(MediaUrl), o => o.MediaUrl, (o, v) => o.MediaUrl = v);
		IsPausedProperty = AvaloniaProperty.Register<MediaPlayerControl, bool>(nameof(IsPaused));
		ShowTransportControlsProperty = AvaloniaProperty.Register<MediaPlayerControl, bool>(nameof(ShowTransportControls), true);
	}

	#endregion

	#region Properties

	/// <summary>
	/// When true, freezes the video surface as a snapshot underlay and hides the native host
	/// so Avalonia content can paint over this region. Also pauses decoding/audio.
	/// </summary>
	public bool IsPaused
	{
		get => GetValue(IsPausedProperty);
		set => SetValue(IsPausedProperty, value);
	}

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

	public bool ShowTransportControls
	{
		get => GetValue(ShowTransportControlsProperty);
		set => SetValue(ShowTransportControlsProperty, value);
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

	public void StopVideo()
	{
		_timer.Stop();
		_mediaPlayerAdapter.Stop();
		_activeSource = null;
		IsPaused = false;
		ResetTransport();
	}

	protected virtual void OnAdapterClosed()
	{
		AdapterClosed?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void OnAdapterInitialized()
	{
		AdapterInitialized?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_mediaPlayerAdapter.Initialize(_nativeHost.NativeHost);
		// Android publishes AndroidViewControlHandle during Initialize — recreate native child.
		_nativeHost.RefreshPlatformSurface();
		_adapterInitialized = true;
		UpdateTransportState();
		UpdateVolumeGlyph();
		base.OnAttachedToVisualTree(e);

		if (_playbackPending && !string.IsNullOrEmpty(_activeSource))
		{
			RestartOrPlay();
		}
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_timer.Stop();
		base.OnDetachedFromVisualTree(e);
		_mediaPlayerAdapter.Dispose();
		_adapterInitialized = false;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if ((change.Property == ShowTransportControlsProperty) && (_transportBar != null))
		{
			_transportBar.IsVisible = ShowTransportControls;
		}
		else if ((change.Property == IsPausedProperty) && (_nativeHost != null))
		{
			_nativeHost.IsPaused = change.GetNewValue<bool>();
			if (change.GetNewValue<bool>())
			{
				_timer.Stop();
			}
			else if (_mediaPlayerAdapter.State == MediaPlaybackState.Playing)
			{
				_timer.Start();
			}

			UpdateTransportState();
		}
	}

	private static string FormatTime(TimeSpan value)
	{
		if (value < TimeSpan.Zero)
		{
			value = TimeSpan.Zero;
		}

		return value.ToString(@"h\:mm\:ss");
	}

	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
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
			_suppressVolumeUpdate = true;
			_mediaPlayerAdapter.Volume = _volumeSlider.Value;
			_suppressVolumeUpdate = false;

			// Preserve aspect by default; fill only when toggle is on.
			_mediaPlayerAdapter.SetVideoStretch(_fillMode);

			RefreshTransportFromAdapter();
			if (!IsPaused)
			{
				_timer.Start();
				_nativeHost.RequestWarmUnderlay();
			}
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

	private void OnAspectClick(object sender, RoutedEventArgs e)
	{
		_fillMode = !_fillMode;
		_mediaPlayerAdapter.SetVideoStretch(_fillMode);
	}

	private void OnControlSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (!IsPaused)
		{
			_mediaPlayerAdapter?.UpdateVideoLayout();
		}
	}

	private void OnMuteClick(object sender, RoutedEventArgs e)
	{
		_mediaPlayerAdapter.IsMuted = !_mediaPlayerAdapter.IsMuted;
		UpdateVolumeGlyph();
	}

	private void OnPlayPauseClick(object sender, RoutedEventArgs e)
	{
		if (IsPaused)
		{
			IsPaused = false;
		}

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

	private void OnPlaybackStarted()
	{
		// Always apply stretch policy (default false = preserve aspect ratio).
		_mediaPlayerAdapter.SetVideoStretch(_fillMode);

		if (!IsPaused)
		{
			_timer.Start();
		}

		UpdateVolumeGlyph();
		UpdateTransportState();
	}

	private void OnSeekPointerPressed(object sender, PointerPressedEventArgs e)
	{
		_isSeeking = true;
	}

	private void OnSeekPointerReleased(object sender, PointerReleasedEventArgs e)
	{
		_mediaPlayerAdapter.Position = TimeSpan.FromSeconds(_seekSlider.Value);
		_isSeeking = false;
	}

	private void OnSeekValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (_suppressSeekUpdate)
		{
			return;
		}

		_positionText.Text = FormatTime(TimeSpan.FromSeconds(e.NewValue));
	}

	private void OnTimerTick(object sender, EventArgs e)
	{
		RefreshTransportFromAdapter();
	}

	private void OnVolumeValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (_suppressVolumeUpdate)
		{
			return;
		}

		_mediaPlayerAdapter.Volume = e.NewValue;

		if (_mediaPlayerAdapter.IsMuted && (e.NewValue > 0))
		{
			_mediaPlayerAdapter.IsMuted = false;
		}

		UpdateVolumeGlyph();
	}

	private void RefreshTransportFromAdapter()
	{
		var duration = _mediaPlayerAdapter.Duration;
		var position = _mediaPlayerAdapter.Position;
		var total = duration.TotalSeconds;

		if ((total > 0) && (Math.Abs(_seekSlider.Maximum - total) > 0.001))
		{
			_seekSlider.Maximum = total;
		}

		_durationText.Text = FormatTime(duration);

		if (!_isSeeking)
		{
			_suppressSeekUpdate = true;
			_seekSlider.Value = Math.Min(position.TotalSeconds, Math.Max(_seekSlider.Maximum, 0));
			_suppressSeekUpdate = false;
			_positionText.Text = FormatTime(position);
		}

		UpdateVolumeGlyph();
		UpdateTransportState();
	}

	private void ResetTransport()
	{
		_suppressSeekUpdate = true;
		_seekSlider.Value = 0;
		_suppressSeekUpdate = false;
		_positionText.Text = FormatTime(TimeSpan.Zero);
		_durationText.Text = FormatTime(TimeSpan.Zero);
		UpdateTransportState();
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

	private void UpdateTransportState()
	{
		var playing = _mediaPlayerAdapter.State == MediaPlaybackState.Playing;
		_playIcon.IsVisible = !playing;
		_pauseIcon.IsVisible = playing;
	}

	private void UpdateVolumeGlyph()
	{
		var muted = _mediaPlayerAdapter.IsMuted || (_volumeSlider.Value <= 0);
		_volumeIcon.IsVisible = !muted;
		_muteIcon.IsVisible = muted;
	}

	#endregion

	#region Events

	public event EventHandler AdapterClosed;
	public event EventHandler AdapterInitialized;

	#endregion
}