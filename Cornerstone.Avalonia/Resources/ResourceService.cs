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
		if (TryGet<Color>(key, out var value))
		{
			return value;
		}
			
		return Colors.Black;
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

	public static StreamGeometry GetSvg(string key)
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
			Data = GetSvg(key)
		};
		return response;
	}

	public static bool TryGet<T>(string key, out T value, T defaultValue = default)
	{
		if (Application.Current?.TryGetResource(key, out var found) == true)
		{
			if (found == null)
			{
				value = defaultValue;
				return false;
			}

			value = (T) found;
			return true;
		}
		
		if (Application.Current?.Styles.Resources.TryGetResource(key, null, out found) == true)
		{
			if (found == null)
			{
				value = defaultValue;
				return false;
			}

			value = (T) found;
			return true;
		}

		value = defaultValue;
		return false;
	}

	#endregion
}