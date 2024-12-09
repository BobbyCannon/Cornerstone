#region References

using Avalonia;

#endregion

namespace Cornerstone.Avalonia;

public static class AppBuilderExtension
{
	#region Methods

	public static AppBuilder WithCornerstoneFont(this AppBuilder appBuilder)
	{
		return appBuilder.ConfigureFonts(fontManager => { fontManager.AddFontCollection(new CornerstoneFontCollection()); });
	}

	#endregion
}