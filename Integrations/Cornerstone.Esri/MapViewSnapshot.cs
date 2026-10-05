#region References

using System;
using System.IO;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.NativeHosts;
#if ANDROID || IOS || WINDOWS
using Esri.ArcGISRuntime.UI.Controls;
#endif

#endregion

namespace Cornerstone.Esri;

internal static class MapViewSnapshot
{
	#region Methods

	#if ANDROID || IOS || WINDOWS
	public static async Task<NativeSurfaceSnapshot> CaptureAsync(GeoView geoView, NativeSurfaceSnapshotOptions options = null)
	{
		if (geoView == null)
		{
			return NativeSurfaceSnapshot.Failed("MapView is not available.");
		}

		try
		{
			var runtimeImage = await geoView.ExportImageAsync().ConfigureAwait(true);
			if (runtimeImage == null)
			{
				return NativeSurfaceSnapshot.Failed("ExportImageAsync returned no image.");
			}

			await using var stream = await runtimeImage.GetEncodedBufferAsync().ConfigureAwait(true);
			using var memory = new MemoryStream();
			await stream.CopyToAsync(memory).ConfigureAwait(true);
			var pngBytes = memory.ToArray();
			var width = (int) Math.Max(1, runtimeImage.Width);
			var height = (int) Math.Max(1, runtimeImage.Height);
			return NativeSurfaceSnapshot.FromPng(pngBytes, width, height);
		}
		catch (Exception ex)
		{
			return NativeSurfaceSnapshot.Failed(ex.Message);
		}
	}
	#else
	public static Task<NativeSurfaceSnapshot> CaptureAsync(object geoView, NativeSurfaceSnapshotOptions options)
	{
		return Task.FromResult(NativeSurfaceSnapshot.Failed("MapView snapshot is not available on this platform."));
	}
	#endif

	#endregion
}
