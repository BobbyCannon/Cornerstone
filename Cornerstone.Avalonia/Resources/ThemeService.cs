#region References

using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

#endregion

namespace Cornerstone.Avalonia.Resources;

public class ThemeService
{
	#region Constructors

	public ThemeService(Application current)
	{
		var themeResource = GetThemeResource(current);

		if (themeResource?.ContainsKey("Color.Accent") == true)
		{
			themeResource.Remove("Color.Accent");
			themeResource.Add("Color.Accent", Colors.Green);
		}

		ResourceColors = new Dictionary<string, Color>();
		ThemeColors = new Dictionary<string, (Color dark, Color light)>();
	}

	static ThemeService()
	{
		ResourceColorKeys =
		[
			"Color.Accent",
			"Color.Accent.Hovered",
			"Color.Accent.Pressed"
		];

		// <ColorKeys>
		ThemeColorKeys =
		[
			"Color.Badge",
			"Color.Border0",
			"Color.Border1",
			"Color.Border2",
			"Color.Border3",
			"Color.Conflict",
			"Color.Contents",
			"Color.Contents.Hovered",
			"Color.Decorator",
			"Color.DecoratorIcon",
			"Color.FlatButton.Background",
			"Color.FlatButton.Background.Hovered",
			"Color.FlatButton.PrimaryBackground",
			"Color.FlatButton.PrimaryBackgroundHovered",
			"Color.Foreground1",
			"Color.Foreground2",
			"Color.Foreground3",
			"Color.Popup",
			"Color.Switcher.Background",
			"Color.Switcher.Hovered",
			"Color.TextEditor.Background",
			"Color.TextEditor.Foreground",
			"Color.TextEditor.Selection",
			"Color.TitleBar",
			"Color.ToolBar",
			"Color.Window",
			"Color.Window.Border"
		];
		// </ColorKeys>
	}

	#endregion

	#region Properties

	public static string[] ResourceColorKeys { get; }

	public Dictionary<string, Color> ResourceColors { get; }

	public ThemeVariant Theme { get; set; }

	public static string[] ThemeColorKeys { get; }

	public Dictionary<string, (Color dark, Color light)> ThemeColors { get; }

	#endregion

	#region Methods

	private ResourceDictionary GetThemeResource(Application current)
	{
		foreach (var dictionary in current.Resources.MergedDictionaries)
		{
			if (dictionary is not ResourceDictionary rd)
			{
				continue;
			}
			if ((rd.ThemeDictionaries.Count < 1)
				|| rd.ThemeDictionaries[ThemeVariant.Dark] is not ResourceDictionary rdd)
			{
				continue;
			}

			if (rdd.ContainsKey("Color.Window"))
			{
				return rd;
			}
		}

		return null;
	}

	#endregion
}