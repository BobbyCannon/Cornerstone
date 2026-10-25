namespace Cornerstone.Platforms;

/// <summary>
/// Copies a square of screen pixels around the pointer. Called off the UI dispatcher.
/// </summary>
public interface IScreenSampler
{
	#region Properties

	bool IsSupported { get; }

	/// <summary>
	/// Shown when <see cref="IsSupported" /> is false. Empty when sampling is available.
	/// </summary>
	string UnavailableReason { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Fills <paramref name="bgraPixels" /> in top-down BGRA order, length <c> sampleSize * sampleSize * 4 </c>.
	/// The center pixel is returned separately.
	/// </summary>
	bool TryCopySample(int sampleSize, byte[] bgraPixels, out byte alpha, out byte red, out byte green, out byte blue);

	#endregion
}
