#region References

using System;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;

#endregion

namespace Cornerstone.Avalonia.Controls;

internal static class NativeSurfaceSnapshotHelper
{
	#region Methods

	public static NativeSurfaceSnapshot ProcessPng(byte[] pngBytes, int sourceWidth, int sourceHeight, NativeSurfaceSnapshotOptions options)
	{
		if (pngBytes is not { Length: > 0 } || (sourceWidth <= 0) || (sourceHeight <= 0))
		{
			return NativeSurfaceSnapshot.Failed("Empty or invalid snapshot image.");
		}

		options ??= NativeSurfaceSnapshotOptions.Default();
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
			return NativeSurfaceSnapshot.FromPng(pngBytes, sourceWidth, sourceHeight);
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
				return NativeSurfaceSnapshot.FromPng(output.ToArray(), targetWidth, targetHeight);
			}
		}
		catch
		{
			// Fall back to original capture if scaling fails.
			return NativeSurfaceSnapshot.FromPng(pngBytes, sourceWidth, sourceHeight);
		}
	}

	#endregion
}