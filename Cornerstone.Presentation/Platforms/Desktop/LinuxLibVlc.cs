using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Cornerstone.Presentation.Platforms.Desktop;

/// <summary>
/// System LibVLC (libvlc.so.5) used by the Linux media player. The library is not bundled.
/// </summary>
internal static class LinuxLibVlc
{
	internal const int StateNothingSpecial = 0;
	internal const int StateOpening = 1;
	internal const int StateBuffering = 2;
	internal const int StatePlaying = 3;
	internal const int StatePaused = 4;
	internal const int StateStopped = 5;
	internal const int StateEnded = 6;
	internal const int StateError = 7;
	internal const int EventPlaying = 260;
	internal const int EventEndReached = 265;

	private static IntPtr _library;
	private static string _pluginDirectory;
	private static InstanceNew _new;
	private static InstanceRelease _release;
	private static MediaNewPath _mediaNewPath;
	private static MediaNewLocation _mediaNewLocation;
	private static MediaRelease _mediaRelease;
	private static MediaParseWithOptions _mediaParseWithOptions;
	private static MediaGetParsedStatus _mediaGetParsedStatus;
	private static MediaTracksGet _mediaTracksGet;
	private static MediaTracksRelease _mediaTracksRelease;
	private static PlayerNew _playerNew;
	private static PlayerRelease _playerRelease;
	private static PlayerSetMedia _playerSetMedia;
	private static PlayerPlay _playerPlay;
	private static PlayerPause _playerSetPause;
	private static PlayerStop _playerStop;
	private static PlayerGetState _playerGetState;
	private static PlayerGetTime _playerGetTime;
	private static PlayerSetTime _playerSetTime;
	private static PlayerGetLength _playerGetLength;
	private static PlayerSetXWindow _playerSetXWindow;
	private static PlayerEventManager _playerEventManager;
	private static EventAttach _eventAttach;
	private static VideoSetCallbacks _videoSetCallbacks;
	private static VideoSetFormat _videoSetFormat;
	private static AudioSetVolume _audioSetVolume;
	private static AudioGetVolume _audioGetVolume;
	private static AudioSetMute _audioSetMute;
	private static AudioGetMute _audioGetMute;

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void EventCallback(IntPtr eventHandle, IntPtr userData);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate IntPtr VideoLock(IntPtr opaque, IntPtr planes);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void VideoDisplay(IntPtr opaque, IntPtr picture);

	internal static bool TryLoad(out string error)
	{
		error = null;
		if (_library != IntPtr.Zero)
		{
			return true;
		}

		if (!OperatingSystem.IsLinux())
		{
			error = "LibVLC playback is only used on Linux.";
			return false;
		}

		LinuxVideoFrames.PreloadFfmpeg();

		if (!NativeLibrary.TryLoad("libvlc.so.5", out _library)
			&& !NativeLibrary.TryLoad("libvlc.so", out _library))
		{
			error = "Linux playback needs system LibVLC (libvlc.so.5).\n\n" +
				"SteamOS (Desktop Mode). Root is read-only; this does not survive a SteamOS update:\n" +
				"  sudo steamos-readonly disable\n" +
				"  sudo pacman-key --init\n" +
				"  sudo pacman-key --populate archlinux holo\n" +
				"  sudo pacman -S vlc vlc-plugin-ffmpeg\n" +
				"  sudo steamos-readonly enable\n\n" +
				"Debian / Ubuntu: sudo apt install libvlc5 vlc\n" +
				"Fedora: sudo dnf install vlc\n" +
				"Arch: sudo pacman -S vlc vlc-plugin-ffmpeg";
			return false;
		}

		_new = Load<InstanceNew>("libvlc_new");
		_release = Load<InstanceRelease>("libvlc_release");
		_mediaNewPath = Load<MediaNewPath>("libvlc_media_new_path");
		_mediaNewLocation = Load<MediaNewLocation>("libvlc_media_new_location");
		_mediaRelease = Load<MediaRelease>("libvlc_media_release");
		if (NativeLibrary.TryGetExport(_library, "libvlc_media_parse_with_options", out var parse))
		{
			_mediaParseWithOptions = Marshal.GetDelegateForFunctionPointer<MediaParseWithOptions>(parse);
		}

		if (NativeLibrary.TryGetExport(_library, "libvlc_media_get_parsed_status", out var parsedStatus))
		{
			_mediaGetParsedStatus = Marshal.GetDelegateForFunctionPointer<MediaGetParsedStatus>(parsedStatus);
		}

		if (NativeLibrary.TryGetExport(_library, "libvlc_media_tracks_get", out var tracksGet))
		{
			_mediaTracksGet = Marshal.GetDelegateForFunctionPointer<MediaTracksGet>(tracksGet);
		}

		if (NativeLibrary.TryGetExport(_library, "libvlc_media_tracks_release", out var tracksRelease))
		{
			_mediaTracksRelease = Marshal.GetDelegateForFunctionPointer<MediaTracksRelease>(tracksRelease);
		}
		_playerNew = Load<PlayerNew>("libvlc_media_player_new");
		_playerRelease = Load<PlayerRelease>("libvlc_media_player_release");
		_playerSetMedia = Load<PlayerSetMedia>("libvlc_media_player_set_media");
		_playerPlay = Load<PlayerPlay>("libvlc_media_player_play");
		_playerSetPause = Load<PlayerPause>("libvlc_media_player_set_pause");
		_playerStop = Load<PlayerStop>("libvlc_media_player_stop");
		_playerGetState = Load<PlayerGetState>("libvlc_media_player_get_state");
		_playerGetTime = Load<PlayerGetTime>("libvlc_media_player_get_time");
		_playerSetTime = Load<PlayerSetTime>("libvlc_media_player_set_time");
		_playerGetLength = Load<PlayerGetLength>("libvlc_media_player_get_length");
		_playerSetXWindow = Load<PlayerSetXWindow>("libvlc_media_player_set_xwindow");
		_playerEventManager = Load<PlayerEventManager>("libvlc_media_player_event_manager");
		_eventAttach = Load<EventAttach>("libvlc_event_attach");
		_videoSetCallbacks = Load<VideoSetCallbacks>("libvlc_video_set_callbacks");
		_videoSetFormat = Load<VideoSetFormat>("libvlc_video_set_format");
		_audioSetVolume = Load<AudioSetVolume>("libvlc_audio_set_volume");
		_audioGetVolume = Load<AudioGetVolume>("libvlc_audio_get_volume");
		_audioSetMute = Load<AudioSetMute>("libvlc_audio_set_mute");
		_audioGetMute = Load<AudioGetMute>("libvlc_audio_get_mute");
		_pluginDirectory = FindPluginDirectory();
		if (!string.IsNullOrEmpty(_pluginDirectory))
		{
			Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", _pluginDirectory);
		}

		return true;
	}

	internal static IntPtr CreateInstance()
	{
		var args = string.IsNullOrEmpty(_pluginDirectory)
			? new[] { "--no-video-title-show" }
			: new[] { "--no-video-title-show", "--plugin-path=" + _pluginDirectory };
		var pointers = new IntPtr[args.Length];
		var array = Marshal.AllocHGlobal(IntPtr.Size * args.Length);
		try
		{
			for (var i = 0; i < args.Length; i++)
			{
				pointers[i] = Marshal.StringToCoTaskMemUTF8(args[i]);
				Marshal.WriteIntPtr(array, i * IntPtr.Size, pointers[i]);
			}

			return _new(args.Length, array);
		}
		finally
		{
			for (var i = 0; i < pointers.Length; i++)
			{
				if (pointers[i] != IntPtr.Zero)
				{
					Marshal.FreeCoTaskMem(pointers[i]);
				}
			}

			Marshal.FreeHGlobal(array);
		}
	}

	internal static void ReleaseInstance(IntPtr instance)
	{
		if (instance != IntPtr.Zero)
		{
			_release(instance);
		}
	}

	internal static IntPtr CreatePlayer(IntPtr instance) => _playerNew(instance);

	internal static void ReleasePlayer(IntPtr player)
	{
		if (player != IntPtr.Zero)
		{
			_playerRelease(player);
		}
	}

	internal static IntPtr CreateMediaPath(IntPtr instance, string path)
	{
		var native = Marshal.StringToCoTaskMemUTF8(path);
		try
		{
			return _mediaNewPath(instance, native);
		}
		finally
		{
			Marshal.FreeCoTaskMem(native);
		}
	}

	internal static IntPtr CreateMediaLocation(IntPtr instance, string location)
	{
		var native = Marshal.StringToCoTaskMemUTF8(location);
		try
		{
			return _mediaNewLocation(instance, native);
		}
		finally
		{
			Marshal.FreeCoTaskMem(native);
		}
	}

	internal static bool TryGetVideoSize(IntPtr media, out int width, out int height, out int sarNum, out int sarDen)
	{
		width = 0;
		height = 0;
		sarNum = 1;
		sarDen = 1;
		if ((media == IntPtr.Zero) || (_mediaParseWithOptions == null) || (_mediaGetParsedStatus == null) || (_mediaTracksGet == null) || (_mediaTracksRelease == null))
		{
			return false;
		}

		// libvlc_media_parse_network also parses local files. 4 is parsed-done.
		if (_mediaParseWithOptions(media, 1, 5000) != 0)
		{
			return false;
		}

		for (var attempt = 0; attempt < 100; attempt++)
		{
			var status = _mediaGetParsedStatus(media);
			if (status == 4)
			{
				break;
			}

			if ((status == 2) || (status == 3))
			{
				return false;
			}

			Thread.Sleep(20);
		}

		var count = _mediaTracksGet(media, out var tracks);
		if ((tracks == IntPtr.Zero) || (count == 0))
		{
			return false;
		}

		try
		{
			for (uint i = 0; i < count; i++)
			{
				var trackPtr = Marshal.ReadIntPtr(tracks, (int) i * IntPtr.Size);
				if (trackPtr == IntPtr.Zero)
				{
					continue;
				}

				var track = Marshal.PtrToStructure<MediaTrack>(trackPtr);
				if ((track.Type != 1) || (track.Video == IntPtr.Zero))
				{
					continue;
				}

				var video = Marshal.PtrToStructure<VideoTrack>(track.Video);
				width = (int) video.Width;
				height = (int) video.Height;
				if ((width < 2) || (height < 2) || (width > 8192) || (height > 8192))
				{
					continue;
				}

				if (video.SarNum > 0)
				{
					sarNum = (int) video.SarNum;
				}

				if (video.SarDen > 0)
				{
					sarDen = (int) video.SarDen;
				}

				return true;
			}
		}
		finally
		{
			_mediaTracksRelease(tracks, count);
		}

		return false;
	}

	internal static void ReleaseMedia(IntPtr media)
	{
		if (media != IntPtr.Zero)
		{
			_mediaRelease(media);
		}
	}

	internal static void SetMedia(IntPtr player, IntPtr media) => _playerSetMedia(player, media);

	internal static int Play(IntPtr player) => _playerPlay(player);

	internal static void SetPause(IntPtr player, bool pause) => _playerSetPause(player, pause ? 1 : 0);

	internal static void Stop(IntPtr player) => _playerStop(player);

	internal static int GetState(IntPtr player) => _playerGetState(player);

	internal static long GetTime(IntPtr player) => _playerGetTime(player);

	internal static void SetTime(IntPtr player, long milliseconds) => _playerSetTime(player, milliseconds);

	internal static long GetLength(IntPtr player) => _playerGetLength(player);

	internal static void SetXWindow(IntPtr player, uint xid) => _playerSetXWindow(player, xid);

	internal static void AttachEvent(IntPtr player, int eventType, EventCallback callback)
	{
		_eventAttach(_playerEventManager(player), eventType, callback, IntPtr.Zero);
	}

	internal static void SetVideoCallbacks(IntPtr player, VideoLock lockCallback, VideoDisplay displayCallback)
	{
		_videoSetCallbacks(player, lockCallback, IntPtr.Zero, displayCallback, IntPtr.Zero);
	}

	internal static void SetVideoFormat(IntPtr player, string chroma, uint width, uint height, uint pitch)
	{
		var native = Marshal.StringToCoTaskMemUTF8(chroma);
		try
		{
			_videoSetFormat(player, native, width, height, pitch);
		}
		finally
		{
			Marshal.FreeCoTaskMem(native);
		}
	}

	internal static void SetVolume(IntPtr player, int volume) => _audioSetVolume(player, volume);

	internal static int GetVolume(IntPtr player) => _audioGetVolume(player);

	internal static void SetMute(IntPtr player, bool mute) => _audioSetMute(player, mute ? 1 : 0);

	internal static bool GetMute(IntPtr player) => _audioGetMute(player) != 0;

	private static string FindPluginDirectory()
	{
		string libraryPath = null;
		if (dladdr(NativeLibrary.GetExport(_library, "libvlc_new"), out var info) != 0 && (info.FileName != IntPtr.Zero))
		{
			libraryPath = Marshal.PtrToStringUTF8(info.FileName);
		}

		var candidates = new System.Collections.Generic.List<string>();
		if (!string.IsNullOrEmpty(libraryPath))
		{
			var directory = System.IO.Path.GetDirectoryName(libraryPath);
			if (!string.IsNullOrEmpty(directory))
			{
				candidates.Add(System.IO.Path.Combine(directory, "vlc", "plugins"));
				var parent = System.IO.Path.GetDirectoryName(directory);
				if (!string.IsNullOrEmpty(parent))
				{
					candidates.Add(System.IO.Path.Combine(parent, "vlc", "plugins"));
				}
			}
		}

		candidates.Add("/usr/lib/x86_64-linux-gnu/vlc/plugins");
		candidates.Add("/usr/lib/aarch64-linux-gnu/vlc/plugins");
		candidates.Add("/usr/lib64/vlc/plugins");
		candidates.Add("/usr/lib/vlc/plugins");
		foreach (var candidate in candidates)
		{
			if (System.IO.Directory.Exists(candidate))
			{
				return candidate;
			}
		}

		return null;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct DlInfo
	{
		public IntPtr FileName;
		public IntPtr BaseAddress;
		public IntPtr SymbolName;
		public IntPtr SymbolAddress;
	}

	[DllImport("libdl.so.2")]
	private static extern int dladdr(IntPtr address, out DlInfo info);

	private static T Load<T>(string name) where T : Delegate
	{
		return Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(_library, name));
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate IntPtr InstanceNew(int argc, IntPtr argv);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void InstanceRelease(IntPtr instance);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate IntPtr MediaNewPath(IntPtr instance, IntPtr path);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate IntPtr MediaNewLocation(IntPtr instance, IntPtr location);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void MediaRelease(IntPtr media);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int MediaParseWithOptions(IntPtr media, int flags, int timeout);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int MediaGetParsedStatus(IntPtr media);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate uint MediaTracksGet(IntPtr media, out IntPtr tracks);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void MediaTracksRelease(IntPtr tracks, uint count);

	[StructLayout(LayoutKind.Sequential)]
	private struct MediaTrack
	{
		public uint Codec;
		public uint OriginalFourcc;
		public int Id;
		public int Type;
		public int Profile;
		public int Level;
		public IntPtr Video;
		public uint Bitrate;
		public IntPtr Language;
		public IntPtr Description;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct VideoTrack
	{
		public uint Height;
		public uint Width;
		public uint SarNum;
		public uint SarDen;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate IntPtr PlayerNew(IntPtr instance);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void PlayerRelease(IntPtr player);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void PlayerSetMedia(IntPtr player, IntPtr media);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int PlayerPlay(IntPtr player);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void PlayerPause(IntPtr player, int pause);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void PlayerStop(IntPtr player);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int PlayerGetState(IntPtr player);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate long PlayerGetTime(IntPtr player);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void PlayerSetTime(IntPtr player, long time);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate long PlayerGetLength(IntPtr player);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void PlayerSetXWindow(IntPtr player, uint xid);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate IntPtr PlayerEventManager(IntPtr player);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void EventAttach(IntPtr manager, int eventType, EventCallback callback, IntPtr userData);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void VideoSetCallbacks(IntPtr player, VideoLock lockCallback, IntPtr unlock, VideoDisplay display, IntPtr opaque);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void VideoSetFormat(IntPtr player, IntPtr chroma, uint width, uint height, uint pitch);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int AudioSetVolume(IntPtr player, int volume);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int AudioGetVolume(IntPtr player);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void AudioSetMute(IntPtr player, int mute);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int AudioGetMute(IntPtr player);
}
