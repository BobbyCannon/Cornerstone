#region References

using System;
using System.IO;
using Cornerstone.Data;
using Cornerstone.MediaPlayer.Keystone;
using Cornerstone.MediaPlayer.Keystone.State;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Platform;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.MediaPlayer;

[SourceReflection]
[Notifiable(["*"])]
public partial class AppViewModel : ApplicationViewModel
{
	#region Constants

	public const string OnlineSampleUrl =
		"https://download.blender.org/peach/trailer/trailer_480p.mov";

	public const string SampleAssetUri = "csres://Cornerstone.MediaPlayer/Assets/BigBuckBunny.mp4";

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AppViewModel(
		AppState state,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher)
		: base(dependencyProvider, dispatcher)
	{
		State = state;
		MediaUrl = string.Empty;
		PageTitle = "Cornerstone Media Player";
		StatusText = "Ready";
	}

	#endregion

	#region Properties

	[Notify]
	public partial string MediaUrl { get; set; }

	[Notify]
	public partial string PageTitle { get; set; }

	public AppSettings Settings => State.Settings;

	public AppState State { get; }

	[Notify]
	public partial string StatusText { get; set; }

	#endregion

	#region Methods

	public override void LoadLifecycle()
	{
		base.LoadLifecycle();
		MediaUrl = Settings.LastMediaUrl ?? string.Empty;
	}

	[RelayCommand]
	public void Play()
	{
		if (string.IsNullOrWhiteSpace(MediaUrl))
		{
			PlaySample();
			return;
		}

		RequestPlayback();
	}

	[RelayCommand]
	public void PlaySample()
	{
		try
		{
			MediaUrl = EnsureSampleAssetFile();
			RequestPlayback();
		}
		catch (Exception ex)
		{
			StatusText = $"Sample asset failed ({ex.Message}). Trying online URL…";
			MediaUrl = OnlineSampleUrl;
			RequestPlayback();
		}
	}

	public override void StartLifecycle()
	{
		base.StartLifecycle();
		State.Settings.ApplyTheme();
		if (string.IsNullOrWhiteSpace(MediaUrl))
		{
			try
			{
				MediaUrl = EnsureSampleAssetFile();
				StatusText = "Ready — press Play sample.";
			}
			catch (Exception ex)
			{
				MediaUrl = OnlineSampleUrl;
				StatusText = $"Embedded asset unavailable ({ex.Message}); online sample URL prefilled.";
			}
		}
	}

	[RelayCommand]
	public void Stop()
	{
		StopRequested?.Invoke(this, EventArgs.Empty);
		StatusText = "Stopped";
	}

	[RelayCommand]
	public void UseOnlineSample()
	{
		MediaUrl = OnlineSampleUrl;
		RequestPlayback();
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		base.OnPropertyChanged(propertyName, oldValue, newValue);

		if (propertyName == nameof(MediaUrl))
		{
			Settings.LastMediaUrl = MediaUrl;
		}
	}

	private static string EnsureSampleAssetFile()
	{
		var directory = Path.Combine(Path.GetTempPath(), "Cornerstone.MediaPlayer");
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

	private void RequestPlayback()
	{
		StatusText = $"Playing: {MediaUrl}";
		PlaybackRequested?.Invoke(this, EventArgs.Empty);
	}

	#endregion

	#region Events

	public event EventHandler PlaybackRequested;

	public event EventHandler StopRequested;

	#endregion
}
