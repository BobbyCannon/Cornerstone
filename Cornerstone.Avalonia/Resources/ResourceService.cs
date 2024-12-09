#region References

using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Path = Avalonia.Controls.Shapes.Path;

#endregion

namespace Cornerstone.Avalonia.Resources;

public static class ResourceService
{
	#region Methods

	public static T Get<T>(string key)
	{
		var resource = Application.Current?.FindResource(key);
		if ((resource == AvaloniaProperty.UnsetValue) && Debugger.IsAttached)
		{
			Debugger.Break();
		}

		return resource is T response ? response : default;
	}

	public static IBrush GetBrush(string key)
	{
		return Get<IBrush>(key);
	}

	public static Color GetColor(string key)
	{
		return TryGet(key, out var value, Colors.Black)
			? value
			: Colors.Black;
	}

	public static IBrush GetColorAsBrush(string key)
	{
		TryGet(key, out var value, Colors.Black);
		return new SolidColorBrush(value);
	}

	public static FontFamily GetFontFamily(string key)
	{
		var response = Application.Current?.FindResource(key) as FontFamily;
		return response;
	}

	public static StreamGeometry GetSvgImage(string key)
	{
		var response = Application.Current?.FindResource(key) as StreamGeometry;
		return response;
	}

	public static Path GetSvgPath(string key)
	{
		var response = new Path
		{
			Width = 12,
			Height = 12,
			Stretch = Stretch.Uniform,
			Data = GetSvgImage(key)
		};
		return response;
	}

	public static bool TryGet<T>(string key, out T value, T defaultValue = default)
	{
		if (!Application.Current.TryGetResource(key, out var found))
		{
			value = defaultValue;
			return false;
		}

		if (found == null)
		{
			value = defaultValue;
			return false;
		}

		value = (T) found;
		return true;
	}

	#endregion
}