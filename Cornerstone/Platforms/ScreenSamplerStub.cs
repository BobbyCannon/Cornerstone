#region References

using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Platforms;

/// <summary>
/// Used where the operating system does not let an app read pixels outside its own windows.
/// </summary>
[SourceReflection]
public sealed class ScreenSamplerStub : IScreenSampler
{
	#region Properties

	public bool IsSupported => false;

	public string UnavailableReason => "This platform cannot sample the screen.";

	#endregion

	#region Methods

	public bool TryCopySample(int sampleSize, byte[] bgraPixels, out byte alpha, out byte red, out byte green, out byte blue)
	{
		alpha = 0;
		red = 0;
		green = 0;
		blue = 0;
		return false;
	}

	#endregion
}