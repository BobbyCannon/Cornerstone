#region References

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Storage;
using Cornerstone.Presentation.Threading;
using Cornerstone.Reflection;
using LibVLCSharp.Shared;

#endregion

namespace Cornerstone.Sample.Tabs.Media;

/// <summary>
/// LibVLC VideoView sample. Uses the same extracted Big Buck Bunny file as Media Player,
/// with the same transport bar over the native surface.
/// </summary>
[SourceReflection]
public partial class TabMediaVlc : UserControl
{
	#region Constants

	public const string HeaderName = "VLC";

	#endregion

	#region Fields

	private DispatcherTimer _timer;

	#endregion

	#region Constructors

	public TabMediaVlc()
	{
		MediaUrl = string.Empty;
		StatusText = "Ready — press Play sample for the hole-emergence clip (~9s).";
		DataContext = this;
		InitializeComponent();

		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(250)
		};
		_timer.Tick += OnTimerTick;
		Bar.PlayPauseRequested += OnPlayPauseRequested;
		Bar.MuteRequested += OnMuteRequested;
		Bar.SeekCompleted += OnSeekCompleted;
		Bar.VolumeChanged += OnVolumeChanged;
	}

	#endregion

	#region Properties

	[Notify]
	public partial string MediaUrl { get; set; }

	[Notify]
	public partial string StatusText { get; set; }

	#endregion

	#region Methods

	[RelayCommand]
	public async Task OpenFile()
	{
		var topLevel = TopLevel.GetTopLevel(this);
		if (topLevel == null)
		{
			return;
		}

		var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Open media file",
			AllowMultiple = false,
			FileTypeFilter =
			[
				new FilePickerFileType("Video")
				{
					Patterns = ["*.mp4", "*.mkv", "*.avi", "*.mov", "*.wmv", "*.webm"]
				},
				FilePickerFileTypes.All
			]
		});

		var file = files.FirstOrDefault();
		if (file == null)
		{
			return;
		}

		var path = file.TryGetLocalPath();
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		MediaUrl = path;
		PlayCurrentSource();
	}

	[RelayCommand]
	public void Play()
	{
		if (string.IsNullOrWhiteSpace(MediaUrl))
		{
			PlaySample();
			return;
		}

		PlayCurrentSource();
	}

	[RelayCommand]
	public void PlaySample()
	{
		try
		{
			MediaUrl = TabMediaPlayer.EnsureSampleAssetFile();
			PlayCurrentSource();
		}
		catch (Exception ex)
		{
			StatusText = $"Sample asset failed ({ex.Message}). Trying online URL…";
			MediaUrl = TabMediaPlayer.OnlineSampleUrl;
			PlayCurrentSource();
		}
	}

	[RelayCommand]
	public void Stop()
	{
		_timer.Stop();
		Player.Stop();
		ResetTransport();
		StatusText = "Stopped";
	}

	[RelayCommand]
	public void UseOnlineSample()
	{
		MediaUrl = TabMediaPlayer.OnlineSampleUrl;
		PlayCurrentSource();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		if (string.IsNullOrWhiteSpace(MediaUrl))
		{
			try
			{
				MediaUrl = TabMediaPlayer.EnsureSampleAssetFile();
				StatusText = "Ready — embedded hole + rabbit emerging (~0.5 MB, ~9s). Press Play.";
			}
			catch (Exception ex)
			{
				MediaUrl = TabMediaPlayer.OnlineSampleUrl;
				StatusText = $"Embedded asset unavailable ({ex.Message}); online sample URL prefilled.";
			}
		}
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_timer.Stop();
		base.OnDetachedFromVisualTree(e);
	}

	private void OnMuteRequested(object sender, EventArgs e)
	{
		var mediaPlayer = Player.MediaPlayer;
		if (mediaPlayer == null)
		{
			return;
		}

		mediaPlayer.Mute = !mediaPlayer.Mute;
		Bar.IsMuted = mediaPlayer.Mute;
	}

	private void OnPlayPauseRequested(object sender, EventArgs e)
	{
		var mediaPlayer = Player.MediaPlayer;
		if ((mediaPlayer != null) && mediaPlayer.IsPlaying)
		{
			mediaPlayer.Pause();
			RefreshTransport();
			return;
		}

		if ((mediaPlayer != null) && (mediaPlayer.State == VLCState.Paused))
		{
			mediaPlayer.Play();
			_timer.Start();
			RefreshTransport();
			return;
		}

		Play();
	}

	private void OnSeekCompleted(object sender, PlayBarSeekEventArgs e)
	{
		var mediaPlayer = Player.MediaPlayer;
		if (mediaPlayer == null)
		{
			return;
		}

		mediaPlayer.Time = (long) e.Position.TotalMilliseconds;
	}

	private void OnTimerTick(object sender, EventArgs e)
	{
		RefreshTransport();
	}

	private void OnVolumeChanged(object sender, PlayBarVolumeEventArgs e)
	{
		var mediaPlayer = Player.MediaPlayer;
		if (mediaPlayer == null)
		{
			return;
		}

		mediaPlayer.Volume = (int) Math.Clamp(e.Volume * 100, 0, 100);
		if (mediaPlayer.Mute && (e.Volume > 0))
		{
			mediaPlayer.Mute = false;
		}

		Bar.IsMuted = mediaPlayer.Mute;
	}

	private void PlayCurrentSource()
	{
		var text = MediaUrl?.Trim();
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		Uri source;
		if (File.Exists(text))
		{
			source = new Uri(text);
		}
		else if (!Uri.TryCreate(text, UriKind.Absolute, out source))
		{
			StatusText = "Enter a file path or an absolute URL.";
			return;
		}

		Player.Source = source;
		Player.Play();
		_timer.Start();
		StatusText = "Playing";
		RefreshTransport();
	}

	private void RefreshTransport()
	{
		var mediaPlayer = Player.MediaPlayer;
		if (mediaPlayer == null)
		{
			return;
		}

		Bar.Duration = TimeSpan.FromMilliseconds(Math.Max(0, mediaPlayer.Length));
		Bar.Position = TimeSpan.FromMilliseconds(Math.Max(0, mediaPlayer.Time));
		Bar.IsPlaying = mediaPlayer.IsPlaying;
		Bar.IsMuted = mediaPlayer.Mute || (mediaPlayer.Volume <= 0);
		Bar.Volume = Math.Clamp(mediaPlayer.Volume / 100d, 0, 1);
	}

	private void ResetTransport()
	{
		Bar.Duration = TimeSpan.Zero;
		Bar.Position = TimeSpan.Zero;
		Bar.IsPlaying = false;
	}

	#endregion
}
