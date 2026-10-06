#region References

using System;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Platform.Storage;

#endregion

namespace Cornerstone.MediaPlayer.Views;

public partial class MainView : UserControl
{
	#region Constructors

	public MainView()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	protected override void OnLoaded(RoutedEventArgs e)
	{
		if (DataContext is AppViewModel viewModel)
		{
			viewModel.PlaybackRequested += OnPlaybackRequested;
			viewModel.StopRequested += OnStopRequested;
		}

		base.OnLoaded(e);
	}

	protected override void OnUnloaded(RoutedEventArgs e)
	{
		if (DataContext is AppViewModel viewModel)
		{
			viewModel.PlaybackRequested -= OnPlaybackRequested;
			viewModel.StopRequested -= OnStopRequested;
		}

		base.OnUnloaded(e);
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
			|| value.StartsWith("csres://", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return value.Contains(':') || value.StartsWith('/') || value.StartsWith('\\');
	}

	private async void OpenFileOnClick(object sender, RoutedEventArgs e)
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
				new FilePickerFileType("Media")
				{
					Patterns = ["*.mp4", "*.mkv", "*.avi", "*.mov", "*.wmv", "*.webm", "*.mp3", "*.wav", "*.m4a"]
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
		if (string.IsNullOrWhiteSpace(path) || DataContext is not AppViewModel viewModel)
		{
			return;
		}

		viewModel.MediaUrl = path;
		PlayCurrentSource();
	}

	private void OnPlaybackRequested(object sender, EventArgs e)
	{
		PlayCurrentSource();
	}

	private void OnStopRequested(object sender, EventArgs e)
	{
		Player.StopVideo();
	}

	private void PlayCurrentSource()
	{
		if (DataContext is not AppViewModel viewModel)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(viewModel.MediaUrl))
		{
			return;
		}

		try
		{
			if (IsLocalFilePath(viewModel.MediaUrl))
			{
				Player.PlayVideoFile(viewModel.MediaUrl);
			}
			else
			{
				Player.PlayVideo(viewModel.MediaUrl);
			}

			viewModel.StatusText = $"Playing: {viewModel.MediaUrl}";
		}
		catch (Exception ex)
		{
			viewModel.StatusText = ex.Message;
		}
	}

	#endregion
}
