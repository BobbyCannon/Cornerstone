#region References

using Avalonia;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Result of capturing a WebView surface for use as an Avalonia placeholder overlay.
/// </summary>
public class WebViewSnapshot
{
	#region Constructors

	public WebViewSnapshot(bool success, byte[] pngBytes = null, PixelSize pixelSize = default, string error = null)
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

	public static WebViewSnapshot Failed(string error = null)
	{
		return new WebViewSnapshot(false, error: error);
	}

	public static WebViewSnapshot FromPng(byte[] pngBytes, int width, int height)
	{
		if (pngBytes is not { Length: > 0 } || (width <= 0) || (height <= 0))
		{
			return Failed("Empty or invalid snapshot image.");
		}

		return new WebViewSnapshot(true, pngBytes, new PixelSize(width, height));
	}

	#endregion
}