#region References

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Path = Avalonia.Controls.Shapes.Path;

#endregion

namespace Cornerstone.Avalonia.Resources;

public static class ResourceService
{
	#region Methods

	public static Path CreateMenuIcon(string key)
	{
		var response = new Path
		{
			Width = 12,
			Height = 12,
			Stretch = Stretch.Uniform,
			Data = GetMenuIcon(key)
		};
		return response;
	}

	public static T Get<T>(string key)
	{
		var resource = Application.Current?.FindResource(key);
		return resource is T response ? response : default;
	}

	public static IBrush GetBrush(string key)
	{
		var response = Application.Current?.FindResource(key) as IBrush;
		return response;
	}

	public static FontFamily GetFontFamily(string key)
	{
		var response = Application.Current?.FindResource(key) as FontFamily;
		return response;
	}

	public static StreamGeometry GetMenuIcon(string key)
	{
		var response = Application.Current?.FindResource(key) as StreamGeometry;
		return response;
	}

	#endregion
}