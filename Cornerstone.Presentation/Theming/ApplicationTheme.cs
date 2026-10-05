#region References

using Cornerstone.Presentation.Theme.Theming;

#endregion

namespace Cornerstone.Presentation;

public static class ApplicationTheme
{
	#region Methods

	public static IApplicationTheme GetCurrent()
	{
		var current = Application.Current;
		if (current == null)
		{
			return null;
		}

		foreach (var style in current.Styles)
		{
			if (style is IApplicationTheme theme)
			{
				return theme;
			}
		}

		return null;
	}

	#endregion
}

/// <summary>
/// Live application theme (implemented by CornerstoneTheme in the Theme assembly).
/// </summary>
public interface IApplicationTheme
{
	#region Properties

	ThemeColor ThemeColor { get; set; }

	ThemeDensity ThemeDensity { get; set; }

	ThemeMode ThemeMode { get; set; }

	#endregion
}