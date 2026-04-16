#region References

using Avalonia;

#endregion

namespace Cornerstone.Avalonia.Platforms;

internal static class GlobalAppBuilder
{
	#region Methods

	public static AppBuilder UseCornerstone(AppBuilder appBuilder)
	{
		return appBuilder;
	}

	#endregion
}