namespace Cornerstone.Avalonia.Themes;

/// <summary>
/// App-wide UI text density. Maps to ControlFontSize / ControlFontSizeSmall theme tokens
/// via <see cref="CornerstoneTheme.SelectThemeDensity" />.
/// </summary>
public enum ThemeDensity
{
	/// <summary>
	/// Smaller chrome and lists (12 / 11).
	/// </summary>
	Compact = 0,

	/// <summary>
	/// Default density (14 / 12).
	/// </summary>
	Normal = 1,

	/// <summary>
	/// Larger chrome and lists (16 / 14).
	/// </summary>
	Large = 2
}