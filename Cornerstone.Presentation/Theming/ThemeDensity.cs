namespace Cornerstone.Presentation.Theme.Theming;

/// <summary>
/// App-wide UI text density. Maps to ControlFontSize / ControlFontSizeSmall / ControlFontSizeLarge
/// via application theme density (CornerstoneTheme.SelectThemeDensity).
/// </summary>
public enum ThemeDensity
{
	/// <summary>
	/// Smaller chrome and lists (Normal tokens minus the density step).
	/// </summary>
	Compact = 0,

	/// <summary>
	/// Default density. Sizes match Theme.Constants.axaml.
	/// </summary>
	Normal = 1,

	/// <summary>
	/// Larger chrome and lists (Normal tokens plus the density step).
	/// </summary>
	Large = 2
}