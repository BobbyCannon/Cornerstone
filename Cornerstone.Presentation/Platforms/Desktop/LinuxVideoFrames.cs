using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Cornerstone.Presentation.Platforms.Desktop;

/// <summary>
/// Shared Linux video helpers: FFmpeg 4.4 preload for VLC's H.264 plugin, and letterboxing.
/// </summary>
public static class LinuxVideoFrames
{
	/// <summary>
	/// SteamOS and Arch build VLC 3 against FFmpeg 4.4 in a private directory.
	/// The H.264 decoder plugin does not load unless those libraries are already mapped.
	/// </summary>
	public static void PreloadFfmpeg()
	{
		var dirs = new[]
		{
			"/usr/lib/ffmpeg4.4",
			"/usr/lib64/ffmpeg4.4"
		};
		foreach (var dir in dirs)
		{
			if (!Directory.Exists(dir))
			{
				continue;
			}

			foreach (var prefix in new[] { "libavutil.so", "libswresample.so", "libswscale.so", "libavcodec.so", "libavformat.so" })
			{
				var soname = ShortestSoname(dir, prefix);
				if (soname != null)
				{
					NativeLibrary.TryLoad(soname, out _);
				}
			}

			return;
		}
	}

	public static void Fit(int videoWidth, int videoHeight, int sarNum, int sarDen, int hostWidth, int hostHeight, out int contentWidth, out int contentHeight, out int offsetX, out int offsetY)
	{
		if (sarNum < 1)
		{
			sarNum = 1;
		}

		if (sarDen < 1)
		{
			sarDen = 1;
		}

		hostWidth &= ~1;
		hostHeight &= ~1;
		var aspect = (videoWidth * (double) sarNum) / (videoHeight * (double) sarDen);
		var hostAspect = (double) hostWidth / hostHeight;
		if (aspect >= hostAspect)
		{
			contentWidth = hostWidth;
			contentHeight = Math.Max(2, (int) Math.Round(hostWidth / aspect) & ~1);
		}
		else
		{
			contentHeight = hostHeight;
			contentWidth = Math.Max(2, (int) Math.Round(hostHeight * aspect) & ~1);
		}

		if (contentWidth > hostWidth)
		{
			contentWidth = hostWidth;
		}

		if (contentHeight > hostHeight)
		{
			contentHeight = hostHeight;
		}

		offsetX = (hostWidth - contentWidth) / 2;
		offsetY = (hostHeight - contentHeight) / 2;
	}

	public static byte[] Letterbox(byte[] frame, int frameWidth, int frameHeight, int hostWidth, int hostHeight, int offsetX, int offsetY)
	{
		if ((frameWidth == hostWidth) && (frameHeight == hostHeight) && (offsetX == 0) && (offsetY == 0))
		{
			return frame;
		}

		var dest = new byte[hostWidth * hostHeight * 4];
		for (var i = 3; i < dest.Length; i += 4)
		{
			dest[i] = 0xFF;
		}

		var rowBytes = frameWidth * 4;
		for (var y = 0; y < frameHeight; y++)
		{
			var source = y * rowBytes;
			var target = ((offsetY + y) * hostWidth + offsetX) * 4;
			if ((source + rowBytes > frame.Length) || (target < 0) || (target + rowBytes > dest.Length))
			{
				break;
			}

			Buffer.BlockCopy(frame, source, dest, target, rowBytes);
		}

		return dest;
	}

	public static string MissingH264Plugin()
	{
		foreach (var directory in H264PluginDirectories())
		{
			if (HasH264Plugin(directory))
			{
				return null;
			}
		}

		return "H.264 playback needs the VLC FFmpeg plugin (libavcodec_plugin.so).\n\n" +
			"Debian / Ubuntu: sudo apt install libvlc5 vlc\n" +
			"SteamOS (Desktop Mode). Root is read-only; this does not survive a SteamOS update:\n" +
			"  sudo steamos-readonly disable\n" +
			"  sudo pacman -S vlc-plugin-ffmpeg\n" +
			"  sudo steamos-readonly enable\n\n" +
			"Arch: sudo pacman -S vlc-plugin-ffmpeg";
	}

	/// <summary>
	/// Plugin directories LibVLC is loaded from, including Debian multiarch layouts.
	/// </summary>
	internal static IReadOnlyList<string> H264PluginDirectories()
	{
		var directories = new List<string>
		{
			"/usr/lib/x86_64-linux-gnu/vlc/plugins",
			"/usr/lib/aarch64-linux-gnu/vlc/plugins",
			"/usr/lib64/vlc/plugins",
			"/usr/lib/vlc/plugins"
		};

		if (!Directory.Exists("/usr/lib"))
		{
			return directories;
		}

		foreach (var directory in Directory.GetDirectories("/usr/lib"))
		{
			var plugins = Path.Combine(directory, "vlc", "plugins");
			if (!directories.Contains(plugins))
			{
				directories.Add(plugins);
			}
		}

		return directories;
	}

	internal static bool HasH264Plugin(string pluginsDirectory)
	{
		if (string.IsNullOrEmpty(pluginsDirectory))
		{
			return false;
		}

		return File.Exists(Path.Combine(pluginsDirectory, "codec", "libavcodec_plugin.so"));
	}

	private static string ShortestSoname(string directory, string prefix)
	{
		string soname = null;
		foreach (var file in Directory.GetFiles(directory, prefix + "*"))
		{
			var name = Path.GetFileName(file);
			var dot = name.LastIndexOf('.');
			if ((dot < 0) || (dot == name.Length - 1))
			{
				continue;
			}

			var suffix = name.Substring(dot + 1);
			var digits = suffix.Length > 0;
			for (var i = 0; i < suffix.Length; i++)
			{
				if (!char.IsDigit(suffix[i]))
				{
					digits = false;
					break;
				}
			}

			if (!digits)
			{
				continue;
			}

			if ((soname == null) || (name.Length < Path.GetFileName(soname).Length))
			{
				soname = file;
			}
		}

		return soname;
	}
}
