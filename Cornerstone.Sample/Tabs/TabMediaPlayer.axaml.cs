#region References

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Cornerstone.Avalonia;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

/// <summary>
/// Sample for MediaPlayerControl: embedded sample asset, URL and local file playback,
/// transport chrome, and IsPaused overlay.
/// </summary>
[SourceReflection]
public partial class TabMediaPlayer : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Media Player";

	/// <summary>
	/// Optional online sample: official Peach trailer (bunny visible; network required).
	/// </summary>
	public const string OnlineSampleUrl =
		"https://download.blender.org/peach/trailer/trailer_480p.mov";

	/// <summary>
	/// Embedded hole-emergence Big Buck Bunny cut (~0.5 MB, ~9s H.264) under Assets/BigBuckBunny.mp4.
	/// Just the rabbit hole and the bunny climbing out (not the opening forest or later scenes).
	/// </summary>
	public const string SampleAssetUri = "avares://Cornerstone.Sample/Assets/BigBuckBunny.mp4";

	#endregion

	#region Constructors

	public TabMediaPlayer() : this(AppBootstrap.GetInstance<AppViewModel>())
	{
	}

	[DependencyInjectionConstructor]
	public TabMediaPlayer(AppViewModel viewModel)
	{
		MediaUrl = string.Empty;
		StatusText = "Ready — press Play sample for the hole-emergence clip (~9s).";
		ViewModel = viewModel;

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	[Notify]
	public partial string MediaUrl { get; set; }

	[Notify]
	public partial string StatusText { get; set; }

	public AppViewModel ViewModel { get; }

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

	/// <summary>
	/// Resolves the embedded Big Buck Bunny asset to a temp file path (MF needs a filesystem path)
	/// and starts playback.
	/// </summary>
	[RelayCommand]
	public void PlaySample()
	{
		try
		{
			MediaUrl = EnsureSampleAssetFile();
			PlayCurrentSource();
		}
		catch (Exception ex)
		{
			StatusText = $"Sample asset failed ({ex.Message}). Trying online URL…";
			MediaUrl = OnlineSampleUrl;
			PlayCurrentSource();
		}
	}

	[RelayCommand]
	public void Stop()
	{
		Player.StopVideo();
		StatusText = "Stopped";
	}

	/// <summary>
	/// Loads the public HTTPS sample URL (network required).
	/// </summary>
	[RelayCommand]
	public void UseOnlineSample()
	{
		MediaUrl = OnlineSampleUrl;
		PlayCurrentSource();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
		base.OnAttachedToVisualTree(e);

		// Prefill with the embedded asset path once Avalonia assets are available.
		if (string.IsNullOrWhiteSpace(MediaUrl))
		{
			try
			{
				MediaUrl = EnsureSampleAssetFile();
				StatusText = "Ready — embedded hole + rabbit emerging (~0.5 MB, ~9s). Press Play.";
			}
			catch (Exception ex)
			{
				MediaUrl = OnlineSampleUrl;
				StatusText = $"Embedded asset unavailable ({ex.Message}); online sample URL prefilled.";
			}
		}
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
		base.OnDetachedFromVisualTree(e);
	}

	/// <summary>
	/// Copies the Avalonia resource to a stable temp path so native MFPlay can open a file path.
	/// </summary>
	private static string EnsureSampleAssetFile()
	{
		var directory = Path.Combine(Path.GetTempPath(), "Cornerstone.Sample");
		Directory.CreateDirectory(directory);
		var path = Path.Combine(directory, "BigBuckBunny.mp4");

		using var stream = AssetLoader.Open(new Uri(SampleAssetUri));
		var expectedLength = stream.CanSeek ? stream.Length : -1L;
		if (File.Exists(path)
			&& ((expectedLength < 0) || (new FileInfo(path).Length == expectedLength))
			&& (new FileInfo(path).Length > 0))
		{
			return path;
		}

		if (stream.CanSeek)
		{
			stream.Position = 0;
		}

		using (var file = File.Create(path))
		{
			stream.CopyTo(file);
		}

		return path;
	}

	private static bool IsLocalFilePath(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
			|| value.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase)
			|| value.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return value.Contains(':') || value.StartsWith('/') || value.StartsWith('\\');
	}

	private void PlayCurrentSource()
	{
		if (string.IsNullOrWhiteSpace(MediaUrl))
		{
			return;
		}

		try
		{
			Player.IsPaused = false;

			// Call Play* directly so pressing Play again restarts the same source
			// (assigning MediaUrl is a no-op when the string is unchanged).
			if (IsLocalFilePath(MediaUrl))
			{
				Player.PlayVideoFile(MediaUrl);
			}
			else
			{
				Player.PlayVideo(MediaUrl);
			}

			StatusText = $"Playing: {MediaUrl}";
		}
		catch (Exception ex)
		{
			StatusText = ex.Message;
		}
	}

	private void ResumeOnClick(object sender, RoutedEventArgs e)
	{
		Player.IsPaused = false;
	}

	private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ViewModel.NavigationMenuIsOpen):
			{
				Player.IsPaused = ViewModel.NavigationMenuIsOpen
					&& ViewModel.NavigationMenuDisplayMode
						is SplitViewDisplayMode.Overlay
						or SplitViewDisplayMode.CompactOverlay;
				break;
			}
		}
	}

	#endregion
}