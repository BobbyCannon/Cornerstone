#region References

using Avalonia;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Result of capturing a native surface for use as an Avalonia placeholder underlay.
/// </summary>
public class NativeSurfaceSnapshot
{
	#region Constructors

	public NativeSurfaceSnapshot(bool success, byte[] pngBytes = null, PixelSize pixelSize = default, string error = null)
	{
		Success = success;
		PngBytes = pngBytes;
		PixelSize = pixelSize;
		Error = error;
	}

	#endregion

	#region Properties

	public string Error { get; }

	public PixelSize PixelSize { get; }

	public byte[] PngBytes { get; }

	public bool Success { get; }

	#endregion

	#region Methods

	public static NativeSurfaceSnapshot Failed(string error = null)
	{
		return new NativeSurfaceSnapshot(false, error: error);
	}

	public static NativeSurfaceSnapshot FromPng(byte[] pngBytes, int width, int height)
	{
		if (pngBytes is not { Length: > 0 } || (width <= 0) || (height <= 0))
		{
			return Failed("Empty or invalid snapshot image.");
		}

		return new NativeSurfaceSnapshot(true, pngBytes, new PixelSize(width, height));
	}

	#endregion
}