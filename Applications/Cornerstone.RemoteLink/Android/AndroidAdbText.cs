#region References

using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace Cornerstone.RemoteLink.Android;

/// <summary>
/// Parses adb stdout (devices -l, wm size).
/// </summary>
public static class AndroidAdbText
{
	#region Methods

	public static IReadOnlyList<AndroidDeviceItem> ParseDevices(string output)
	{
		var list = new List<AndroidDeviceItem>();
		if (string.IsNullOrEmpty(output))
		{
			return list;
		}

		using var reader = new StringReader(output);
		string line;
		while ((line = reader.ReadLine()) != null)
		{
			line = line.Trim();
			if ((line.Length == 0) || line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2)
			{
				continue;
			}

			var serial = parts[0];
			var state = parts[1];
			var model = serial;
			foreach (var part in parts)
			{
				if (part.StartsWith("model:", StringComparison.OrdinalIgnoreCase))
				{
					model = part.Substring(6).Replace('_', ' ');
					break;
				}
			}

			list.Add(new AndroidDeviceItem(serial, state, model));
		}

		return list;
	}

	public static void AlignOrientation(int frameWidth, int frameHeight, ref int displayWidth, ref int displayHeight)
	{
		if ((frameWidth <= 0) || (frameHeight <= 0) || (displayWidth <= 0) || (displayHeight <= 0))
		{
			return;
		}

		var frameLandscape = frameWidth > frameHeight;
		var displayLandscape = displayWidth > displayHeight;
		if (frameLandscape == displayLandscape)
		{
			return;
		}

		(displayWidth, displayHeight) = (displayHeight, displayWidth);
	}

	public static bool TryParseWmSize(string output, out int width, out int height)
	{
		width = 0;
		height = 0;
		if (string.IsNullOrEmpty(output))
		{
			return false;
		}

		var physicalWidth = 0;
		var physicalHeight = 0;
		var overrideWidth = 0;
		var overrideHeight = 0;
		using var reader = new StringReader(output);
		string line;
		while ((line = reader.ReadLine()) != null)
		{
			line = line.Trim();
			if (TryParseSizeLine(line, "Override size:", out var parsedWidth, out var parsedHeight))
			{
				overrideWidth = parsedWidth;
				overrideHeight = parsedHeight;
			}
			else if (TryParseSizeLine(line, "Physical size:", out parsedWidth, out parsedHeight))
			{
				physicalWidth = parsedWidth;
				physicalHeight = parsedHeight;
			}
		}

		if ((overrideWidth > 0) && (overrideHeight > 0))
		{
			width = overrideWidth;
			height = overrideHeight;
			return true;
		}

		if ((physicalWidth > 0) && (physicalHeight > 0))
		{
			width = physicalWidth;
			height = physicalHeight;
			return true;
		}

		return false;
	}

	private static bool TryParseSizeLine(string line, string prefix, out int width, out int height)
	{
		width = 0;
		height = 0;
		if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var size = line.Substring(prefix.Length).Trim();
		var x = size.IndexOf('x');
		if (x <= 0)
		{
			return false;
		}

		return int.TryParse(size.AsSpan(0, x), out width)
			&& int.TryParse(size.AsSpan(x + 1), out height)
			&& (width > 0)
			&& (height > 0);
	}

	#endregion
}
