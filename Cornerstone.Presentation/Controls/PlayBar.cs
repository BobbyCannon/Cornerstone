#region References

using System;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;

#endregion

namespace Cornerstone.Presentation.Controls;

/// <summary>
/// Playback chrome: time, seek, play/pause, mute, and volume.
/// The host owns the player and pushes state in. Volume is 0 to 1.
/// </summary>
[TemplatePart(PartHitArea, typeof(Border))]
[TemplatePart(PartBar, typeof(Border))]
[TemplatePart(PartPositionText, typeof(TextBlock))]
[TemplatePart(PartDurationText, typeof(TextBlock))]
[TemplatePart(PartSeekSlider, typeof(Slider))]
[TemplatePart(PartVolumeSlider, typeof(Slider))]
[TemplatePart(PartPlayPauseButton, typeof(Button))]
[TemplatePart(PartMuteButton, typeof(Button))]
[TemplatePart(PartPlayIcon, typeof(Path))]
[TemplatePart(PartPauseIcon, typeof(Path))]
[TemplatePart(PartVolumeIcon, typeof(Path))]
[TemplatePart(PartMuteIcon, typeof(Path))]
public class PlayBar : TemplatedControl
{
	#region Fields

	public const string PartBar = "PART_Bar";
	public const string PartDurationText = "PART_DurationText";
	public const string PartHitArea = "PART_HitArea";
	public const string PartMuteButton = "PART_MuteButton";
	public const string PartMuteIcon = "PART_MuteIcon";
	public const string PartPauseIcon = "PART_PauseIcon";
	public const string PartPlayIcon = "PART_PlayIcon";
	public const string PartPlayPauseButton = "PART_PlayPauseButton";
	public const string PartPositionText = "PART_PositionText";
	public const string PartSeekSlider = "PART_SeekSlider";
	public const string PartVolumeIcon = "PART_VolumeIcon";
	public const string PartVolumeSlider = "PART_VolumeSlider";

	public static readonly StyledProperty<TimeSpan> DurationProperty;
	public static readonly StyledProperty<bool> IsMutedProperty;
	public static readonly StyledProperty<bool> IsPlayingProperty;
	public static readonly StyledProperty<TimeSpan> PositionProperty;
	public static readonly StyledProperty<double> VolumeProperty;

	private Border _bar;
	private bool _chromeOpen;
	private TextBlock _durationText;
	private bool _forceChromeVisible;
	private Border _hitArea;
	private bool _isAdjustingVolume;
	private bool _isPointerOver;
	private bool _isSeeking;
	private Button _muteButton;
	private Path _muteIcon;
	private Path _pauseIcon;
	private Path _playIcon;
	private Button _playPauseButton;
	private TextBlock _positionText;
	private Slider _seekSlider;
	private bool _suppressSeekUpdate;
	private bool _suppressVolumeUpdate;
	private Path _volumeIcon;
	private Slider _volumeSlider;

	#endregion

	#region Constructors

	public PlayBar()
	{
		_chromeOpen = true;
		_forceChromeVisible = false;
		_isAdjustingVolume = false;
		_isPointerOver = false;
		_isSeeking = false;
		_suppressSeekUpdate = false;
		_suppressVolumeUpdate = false;
	}

	static PlayBar()
	{
		DurationProperty = PresentationProperty.Register<PlayBar, TimeSpan>(nameof(Duration));
		IsMutedProperty = PresentationProperty.Register<PlayBar, bool>(nameof(IsMuted));
		IsPlayingProperty = PresentationProperty.Register<PlayBar, bool>(nameof(IsPlaying));
		PositionProperty = PresentationProperty.Register<PlayBar, TimeSpan>(nameof(Position));
		VolumeProperty = PresentationProperty.Register<PlayBar, double>(nameof(Volume), 1d);
	}

	#endregion

	#region Properties

	public TimeSpan Duration
	{
		get => GetValue(DurationProperty);
		set => SetValue(DurationProperty, value);
	}

	/// <summary>
	/// True when the bar is on screen and can take input.
	/// </summary>
	public bool IsChromeOpen => IsVisible && _chromeOpen;

	public bool IsMuted
	{
		get => GetValue(IsMutedProperty);
		set => SetValue(IsMutedProperty, value);
	}

	public bool IsPlaying
	{
		get => GetValue(IsPlayingProperty);
		set => SetValue(IsPlayingProperty, value);
	}

	public TimeSpan Position
	{
		get => GetValue(PositionProperty);
		set => SetValue(PositionProperty, value);
	}

	public double Volume
	{
		get => GetValue(VolumeProperty);
		set => SetValue(VolumeProperty, value);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Keep the bar visible while playback would otherwise hide it. Pass false to return to hover.
	/// </summary>
	public void KeepChromeOpen(bool keep)
	{
		_forceChromeVisible = keep;
		ApplyChrome();
	}

	/// <summary>
	/// Touch has no hover. A tap pins a hidden bar open, and the next tap returns it to hover.
	/// </summary>
	public void TogglePinnedChrome()
	{
		if (_forceChromeVisible)
		{
			_forceChromeVisible = false;
		}
		else if (!_chromeOpen)
		{
			_forceChromeVisible = true;
		}
		else
		{
			return;
		}

		ApplyChrome();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		DetachTemplate();
		_hitArea = e.NameScope.Find<Border>(PartHitArea);
		_bar = e.NameScope.Find<Border>(PartBar);
		_positionText = e.NameScope.Find<TextBlock>(PartPositionText);
		_durationText = e.NameScope.Find<TextBlock>(PartDurationText);
		_seekSlider = e.NameScope.Find<Slider>(PartSeekSlider);
		_volumeSlider = e.NameScope.Find<Slider>(PartVolumeSlider);
		_playPauseButton = e.NameScope.Find<Button>(PartPlayPauseButton);
		_muteButton = e.NameScope.Find<Button>(PartMuteButton);
		_playIcon = e.NameScope.Find<Path>(PartPlayIcon);
		_pauseIcon = e.NameScope.Find<Path>(PartPauseIcon);
		_volumeIcon = e.NameScope.Find<Path>(PartVolumeIcon);
		_muteIcon = e.NameScope.Find<Path>(PartMuteIcon);

		if (_hitArea != null)
		{
			_hitArea.PointerEntered += HitAreaOnPointerEntered;
			_hitArea.PointerExited += HitAreaOnPointerExited;
			_hitArea.Tapped += HitAreaOnTapped;
		}

		if (_playPauseButton != null)
		{
			_playPauseButton.Click += PlayPauseOnClick;
		}

		if (_muteButton != null)
		{
			_muteButton.Click += MuteOnClick;
		}

		if (_seekSlider != null)
		{
			_seekSlider.AddHandler(PointerPressedEvent, SeekOnPointerPressed, RoutingStrategies.Tunnel);
			_seekSlider.AddHandler(PointerReleasedEvent, SeekOnPointerReleased, RoutingStrategies.Tunnel);
			_seekSlider.ValueChanged += SeekOnValueChanged;
		}

		if (_volumeSlider != null)
		{
			_volumeSlider.AddHandler(PointerPressedEvent, VolumeOnPointerPressed, RoutingStrategies.Tunnel);
			_volumeSlider.AddHandler(PointerReleasedEvent, VolumeOnPointerReleased, RoutingStrategies.Tunnel);
			_volumeSlider.ValueChanged += VolumeOnValueChanged;
		}

		ApplyDuration(Duration);
		ApplyPosition(Position);
		ApplyPlaybackGlyph(IsPlaying);
		ApplyVolume(Volume);
		ApplyChrome();
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (_seekSlider == null)
		{
			return;
		}

		if (change.Property == DurationProperty)
		{
			ApplyDuration(change.GetNewValue<TimeSpan>());
		}
		else if (change.Property == PositionProperty)
		{
			ApplyPosition(change.GetNewValue<TimeSpan>());
		}
		else if (change.Property == IsPlayingProperty)
		{
			ApplyPlaybackGlyph(change.GetNewValue<bool>());
			ApplyChrome();
		}
		else if (change.Property == VolumeProperty)
		{
			ApplyVolume(change.GetNewValue<double>());
		}
		else if (change.Property == IsMutedProperty)
		{
			ApplyVolumeGlyph();
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

	private void ApplyChrome()
	{
		if (_bar == null)
		{
			return;
		}

		var shown = _forceChromeVisible || _isPointerOver || _isSeeking || _isAdjustingVolume || !IsPlaying;
		_chromeOpen = shown;
		_bar.Opacity = shown ? 1 : 0;
		// Hidden buttons must not take the tap. The hit area stays live so a mouse can hover back in.
		_bar.IsHitTestVisible = shown;
	}

	private void ApplyDuration(TimeSpan duration)
	{
		if (_seekSlider == null)
		{
			return;
		}

		var total = duration.TotalSeconds;
		if (total < 0)
		{
			total = 0;
		}

		_seekSlider.Maximum = total > 0 ? total : 1;
		_durationText.Text = FormatTime(duration);
	}

	private void ApplyPlaybackGlyph(bool playing)
	{
		if (_playIcon == null)
		{
			return;
		}

		_playIcon.IsVisible = !playing;
		_pauseIcon.IsVisible = playing;
	}

	private void ApplyPosition(TimeSpan position)
	{
		if (_isSeeking)
		{
			return;
		}

		var seconds = position.TotalSeconds;
		if (seconds < 0)
		{
			seconds = 0;
		}

		_suppressSeekUpdate = true;
		_seekSlider.Value = Math.Min(seconds, Math.Max(_seekSlider.Maximum, 0));
		_suppressSeekUpdate = false;
		_positionText.Text = FormatTime(position);
	}

	private void ApplyVolume(double volume)
	{
		if (_isAdjustingVolume)
		{
			return;
		}

		_suppressVolumeUpdate = true;
		_volumeSlider.Value = Math.Clamp(volume, 0, 1);
		_suppressVolumeUpdate = false;
		ApplyVolumeGlyph();
	}

	private void ApplyVolumeGlyph()
	{
		if (_volumeIcon == null)
		{
			return;
		}

		var muted = IsMuted || (_volumeSlider.Value <= 0);
		_volumeIcon.IsVisible = !muted;
		_muteIcon.IsVisible = muted;
	}

	private void DetachTemplate()
	{
		if (_hitArea != null)
		{
			_hitArea.PointerEntered -= HitAreaOnPointerEntered;
			_hitArea.PointerExited -= HitAreaOnPointerExited;
			_hitArea.Tapped -= HitAreaOnTapped;
		}

		if (_playPauseButton != null)
		{
			_playPauseButton.Click -= PlayPauseOnClick;
		}

		if (_muteButton != null)
		{
			_muteButton.Click -= MuteOnClick;
		}

		if (_seekSlider != null)
		{
			_seekSlider.RemoveHandler(PointerPressedEvent, SeekOnPointerPressed);
			_seekSlider.RemoveHandler(PointerReleasedEvent, SeekOnPointerReleased);
			_seekSlider.ValueChanged -= SeekOnValueChanged;
		}

		if (_volumeSlider != null)
		{
			_volumeSlider.RemoveHandler(PointerPressedEvent, VolumeOnPointerPressed);
			_volumeSlider.RemoveHandler(PointerReleasedEvent, VolumeOnPointerReleased);
			_volumeSlider.ValueChanged -= VolumeOnValueChanged;
		}
	}

	private void HitAreaOnPointerEntered(object sender, PointerEventArgs e)
	{
		_isPointerOver = true;
		ApplyChrome();
	}

	private void HitAreaOnPointerExited(object sender, PointerEventArgs e)
	{
		_isPointerOver = false;
		if (_isSeeking || _isAdjustingVolume)
		{
			return;
		}

		ApplyChrome();
	}

	private void HitAreaOnTapped(object sender, TappedEventArgs e)
	{
		// A tap on the buttons is the button. A tap on the hidden bar toggles the pin.
		if ((e.Pointer.Type != PointerType.Touch) || !ReferenceEquals(e.Source, _hitArea))
		{
			return;
		}

		TogglePinnedChrome();
		e.Handled = true;
	}

	private void MuteOnClick(object sender, RoutedEventArgs e)
	{
		MuteRequested?.Invoke(this, EventArgs.Empty);
	}

	private void PlayPauseOnClick(object sender, RoutedEventArgs e)
	{
		PlayPauseRequested?.Invoke(this, EventArgs.Empty);
	}

	private void SeekOnPointerPressed(object sender, PointerPressedEventArgs e)
	{
		_isSeeking = true;
		ApplyChrome();
	}

	private void SeekOnPointerReleased(object sender, PointerReleasedEventArgs e)
	{
		_isSeeking = false;
		_isPointerOver = _hitArea.IsPointerOver;
		SeekCompleted?.Invoke(this, new PlayBarSeekEventArgs(TimeSpan.FromSeconds(_seekSlider.Value)));
		ApplyChrome();
	}

	private void SeekOnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (_suppressSeekUpdate)
		{
			return;
		}

		_positionText.Text = FormatTime(TimeSpan.FromSeconds(e.NewValue));
	}

	private void VolumeOnPointerPressed(object sender, PointerPressedEventArgs e)
	{
		_isAdjustingVolume = true;
		ApplyChrome();
	}

	private void VolumeOnPointerReleased(object sender, PointerReleasedEventArgs e)
	{
		_isAdjustingVolume = false;
		_isPointerOver = _hitArea.IsPointerOver;
		ApplyVolume(Volume);
		ApplyChrome();
	}

	private void VolumeOnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (_suppressVolumeUpdate)
		{
			return;
		}

		SetCurrentValue(VolumeProperty, Math.Clamp(e.NewValue, 0, 1));
		ApplyVolumeGlyph();
		VolumeChanged?.Invoke(this, new PlayBarVolumeEventArgs(Volume));
	}

	#endregion

	#region Events

	public event EventHandler MuteRequested;
	public event EventHandler PlayPauseRequested;
	public event EventHandler<PlayBarSeekEventArgs> SeekCompleted;
	public event EventHandler<PlayBarVolumeEventArgs> VolumeChanged;

	#endregion
}

public sealed class PlayBarSeekEventArgs : EventArgs
{
	#region Constructors

	public PlayBarSeekEventArgs(TimeSpan position)
	{
		Position = position;
	}

	#endregion

	#region Properties

	public TimeSpan Position { get; }

	#endregion
}

public sealed class PlayBarVolumeEventArgs : EventArgs
{
	#region Constructors

	public PlayBarVolumeEventArgs(double volume)
	{
		Volume = volume;
	}

	#endregion

	#region Properties

	public double Volume { get; }

	#endregion
}
