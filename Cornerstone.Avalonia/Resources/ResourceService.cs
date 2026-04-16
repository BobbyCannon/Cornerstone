#region References

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Cornerstone.Avalonia.Themes;
using Path = Avalonia.Controls.Shapes.Path;

#endregion

namespace Cornerstone.Avalonia.Resources;

public static class ResourceService
{
	#region Fields

	public static readonly FuncValueConverter<string, Path> IconAsPath = new(x => TryGetSvgPath(x, out var path) ? path : null);

	#endregion

	#region Constructors

	static ResourceService()
	{
		Ambers = GetColorsAsBrush("Amber").ToArray();
		Blues = GetColorsAsBrush("Blue").ToArray();
		BlueGrays = GetColorsAsBrush("BlueGray").ToArray();
		Browns = GetColorsAsBrush("Brown").ToArray();
		DeepOranges = GetColorsAsBrush("DeepOrange").ToArray();
		DeepPurples = GetColorsAsBrush("DeepPurple").ToArray();
		Grays = GetColorsAsBrush("Gray").ToArray();
		Greens = GetColorsAsBrush("Green").ToArray();
		Indigos = GetColorsAsBrush("Indigo").ToArray();
		Oranges = GetColorsAsBrush("Orange").ToArray();
		Pinks = GetColorsAsBrush("Pink").ToArray();
		Purples = GetColorsAsBrush("Purple").ToArray();
		Reds = GetColorsAsBrush("Red").ToArray();
		Teals = GetColorsAsBrush("Teal").ToArray();

		Amber = GetColorAsBrush("Amber05");
		Blue = GetColorAsBrush("Blue05");
		BlueGray = GetColorAsBrush("BlueGray05");
		Brown = GetColorAsBrush("Brown05");
		DeepOrange = GetColorAsBrush("DeepOrange05");
		DeepPurple = GetColorAsBrush("DeepPurple05");
		Gray = GetColorAsBrush("Gray05");
		Green = GetColorAsBrush("Green05");
		Indigo = GetColorAsBrush("Indigo05");
		Orange = GetColorAsBrush("Orange05");
		Pink = GetColorAsBrush("Pink05");
		Purple = GetColorAsBrush("Purple05");
		Red = GetColorAsBrush("Red05");
		Teal = GetColorAsBrush("Teal05");

		Brushes =
		[
			Amber, Blue, BlueGray, Brown, DeepOrange, DeepPurple, Gray,
			Green, Indigo, Orange, Pink, Purple, Red, Teal
		];
	}

	#endregion

	#region Properties

	public static IBrush Amber { get; }

	public static IBrush[] Ambers { get; }

	public static IBrush Blue { get; }

	public static IBrush BlueGray { get; }

	public static IBrush[] BlueGrays { get; }

	public static IBrush[] Blues { get; }

	public static IBrush Brown { get; }

	public static IBrush[] Browns { get; }

	public static IBrush[] Brushes { get; }

	public static IBrush DeepOrange { get; }

	public static IBrush[] DeepOranges { get; }

	public static IBrush DeepPurple { get; }

	public static IBrush[] DeepPurples { get; }

	public static IBrush Gray { get; }

	public static IBrush[] Grays { get; }

	public static IBrush Green { get; }

	public static IBrush[] Greens { get; }

	public static IBrush Indigo { get; }

	public static IBrush[] Indigos { get; }

	public static IBrush Orange { get; }

	public static IBrush[] Oranges { get; }

	public static IBrush Pink { get; }

	public static IBrush[] Pinks { get; }

	public static IBrush Purple { get; }

	public static IBrush[] Purples { get; }

	public static IBrush Red { get; }

	public static IBrush[] Reds { get; }

	public static IBrush Teal { get; }

	public static IBrush[] Teals { get; }

	#endregion

	#region Methods

	public static T Get<T>(string key)
	{
		var resource = Application.Current?.FindResource(key);
		if ((resource == AvaloniaProperty.UnsetValue) && Debugger.IsAttached)
		{
			#if DEBUG
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			#endif
		}

		return resource is T response ? response : default;
	}

	public static IBrush GetBrush(string key)
	{
		return Get<IBrush>(key);
	}

	public static IBrush GetBrush(ThemeColor color)
	{
		return color switch
		{
			ThemeColor.Amber => Amber,
			ThemeColor.Blue => Blue,
			ThemeColor.BlueGray => BlueGray,
			ThemeColor.Brown => Brown,
			ThemeColor.DeepOrange => DeepOrange,
			ThemeColor.DeepPurple => DeepPurple,
			ThemeColor.Gray => Gray,
			ThemeColor.Green => Green,
			ThemeColor.Indigo => Indigo,
			ThemeColor.Orange => Orange,
			ThemeColor.Pink => Pink,
			ThemeColor.Purple => Purple,
			ThemeColor.Red => Red,
			ThemeColor.Teal => Teal,
			_ => null
		};
	}

	public static Color GetColor(string key, StyledElement control = null)
	{
		return TryGet<Color>(key, out var value, default, control) ? value : Colors.Black;
	}

	public static IBrush GetColorAsBrush(string key, double opacity = 1.0, StyledElement control = null)
	{
		TryGet(key, out var value, Colors.Black, control);
		return new SolidColorBrush(value, opacity);
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

	public static bool TryGet<T>(string key, out T value, T defaultValue = default!, StyledElement control = null)
	{
		if (key == null)
		{
			value = defaultValue;
			return false;
		}

		var theme = control?.ActualThemeVariant
			?? Application.Current?.ActualThemeVariant;

		if ((control?.TryGetResource(key, theme, out var found) == true)
			|| (Application.Current?.TryGetResource(key, theme, out found) == true)
			|| (Application.Current?.Styles.Resources.TryGetResource(key, theme, out found) == true))
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

	public static bool TryGetSvg(string key, out StreamGeometry geometry)
	{
		if (key == null)
		{
			geometry = null;
			return false;
		}

		geometry = Application.Current?.FindResource(key) as StreamGeometry;
		return geometry != null;
	}

	public static bool TryGetSvgPath(string key, out Path path)
	{
		path = TryGetSvg(key, out var data)
			? path = new Path
			{
				Width = 12,
				Height = 12,
				Stretch = Stretch.Uniform,
				Data = data
			}
			: null;
		return path != null;
	}

	private static IEnumerable<IBrush> GetColorsAsBrush(string color)
	{
		for (var i = 0; i < 10; i++)
		{
			yield return GetColorAsBrush($"{color}{i:00}");
		}
	}

	#endregion
}