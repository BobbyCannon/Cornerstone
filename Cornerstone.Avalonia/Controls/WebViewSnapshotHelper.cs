#region References

using System;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;

#endregion

namespace Cornerstone.Avalonia.Controls;

internal static class WebViewSnapshotHelper
{
	#region Methods

	public static WebViewSnapshot ProcessPng(byte[] pngBytes, int sourceWidth, int sourceHeight, WebViewSnapshotOptions options)
	{
		if (pngBytes is not { Length: > 0 } || (sourceWidth <= 0) || (sourceHeight <= 0))
		{
			return WebViewSnapshot.Failed("Empty or invalid snapshot image.");
		}

		options ??= WebViewSnapshotOptions.Default();
		var scale = options.Scale <= 0 ? 1.0 : Math.Clamp(options.Scale, 0.05, 1.0);

		var targetWidth = Math.Max(1, (int) Math.Round(sourceWidth * scale));
		var targetHeight = Math.Max(1, (int) Math.Round(sourceHeight * scale));

		if ((options.MaxWidth > 0) && (targetWidth > options.MaxWidth))
		{
			var factor = (double) options.MaxWidth / targetWidth;
			targetWidth = options.MaxWidth;
			targetHeight = Math.Max(1, (int) Math.Round(targetHeight * factor));
		}

		if ((options.MaxHeight > 0) && (targetHeight > options.MaxHeight))
		{
			var factor = (double) options.MaxHeight / targetHeight;
			targetHeight = options.MaxHeight;
			targetWidth = Math.Max(1, (int) Math.Round(targetWidth * factor));
		}

		if ((targetWidth == sourceWidth) && (targetHeight == sourceHeight))
		{
			return WebViewSnapshot.FromPng(pngBytes, sourceWidth, sourceHeight);
		}

		try
		{
			using var input = new MemoryStream(pngBytes);
			using var source = new Bitmap(input);
			var scaled = source.CreateScaledBitmap(new PixelSize(targetWidth, targetHeight));
			using (scaled)
			using (var output = new MemoryStream())
			{
				scaled.Save(output);
				return WebViewSnapshot.FromPng(output.ToArray(), targetWidth, targetHeight);
			}
		}
		catch
		{
			// Fall back to original capture if scaling fails.
			return WebViewSnapshot.FromPng(pngBytes, sourceWidth, sourceHeight);
		}
	}

	#endregion
}