#region References

using System;
using System.IO;
using System.Linq;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Cornerstone.Media;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.Windows;

[SourceReflection]
public partial class WindowsAudioPlayer : AudioPlayer
{
	#region Fields

	private readonly MediaPlayer _player;
	private readonly IRuntimeInformation _runtimeInformation;
	private readonly SpeechSynthesizer _speech;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public WindowsAudioPlayer(
		IDateTimeProvider dateTimeProvider,
		IRuntimeInformation runtimeInformation,
		IDispatcher dispatcher
	) : base(dateTimeProvider, dispatcher)
	{
		_runtimeInformation = runtimeInformation;
		_player = new MediaPlayer();
		_player.Volume = 1;
		_player.MediaEnded += OnMediaEnded;
		_player.MediaFailed += OnMediaFailed;

		_speech = new SpeechSynthesizer();
		_speech.Voice =
			SpeechSynthesizer.AllVoices.FirstOrDefault(x => x.DisplayName == "Microsoft Mark")
			?? SpeechSynthesizer.AllVoices.FirstOrDefault(x => (x.Gender == VoiceGender.Male) && (x.Language == "en-US"))
			?? SpeechSynthesizer.AllVoices.First();
	}

	#endregion

	#region Properties

	public override int CurrentPosition => (int) _player.PlaybackSession.Position.TotalMilliseconds;

	public override int Duration
	{
		get
		{
			var duration = _player.PlaybackSession.NaturalDuration;
			return duration > TimeSpan.Zero ? (int) duration.TotalMilliseconds : 0;
		}
	}

	#endregion

	#region Methods

	public override void Pause()
	{
		this.Dispatch(() =>
		{
			IsPlaying = false;
			_player.Pause();
		});
	}

	public override void Play()
	{
		this.Dispatch(() =>
		{
			IsPlaying = true;
			_player.Play();
		});
	}

	public override void Seek(double position)
	{
		this.Dispatch(() =>
		{
			var isPlaying = IsPlaying;
			Pause();
			_player.PlaybackSession.Position = TimeSpan.FromSeconds(position);
			if (isPlaying)
			{
				Play();
			}
		});
	}

	public override async void Speak(string message)
	{
		try
		{
			Stop();

			using var stream = await _speech.SynthesizeTextToStreamAsync(message);
			var speechFilePath = Path.Join(_runtimeInformation.ApplicationDataLocation, "speech.wav");
			await using var fileStream = new FileStream(speechFilePath, FileMode.Create);
			stream.AsStreamForRead().CopyTo(fileStream);
			fileStream.Close();
			PlayFile(speechFilePath);
		}
		catch
		{
			// Ignore any issues
		}
	}

	public override void Stop()
	{
		this.Dispatch(() =>
		{
			_player.Pause();
			_player.Source = null;
			IsPlaying = false;
		});
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_player.MediaEnded -= OnMediaEnded;
			_player.MediaFailed -= OnMediaFailed;
			_player.Dispose();
		}

		base.Dispose(disposing);
	}

	protected override void PlayFile(string audioFilePath)
	{
		this.Dispatch(() =>
		{
			Stop();
			_player.Source = MediaSource.CreateFromUri(new Uri(audioFilePath));
			_player.PlaybackSession.Position = TimeSpan.Zero;
			Play();
		});
	}

	protected override void PlayUri(Uri uri)
	{
		this.Dispatch(() =>
		{
			Stop();
			_player.Source = MediaSource.CreateFromUri(uri);
			_player.PlaybackSession.Position = TimeSpan.Zero;
			Play();
		});
	}

	private void OnMediaEnded(MediaPlayer sender, object args)
	{
		OnPlaybackEnded();
	}

	private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
	{
		OnPlaybackEnded();
	}

	#endregion
}
