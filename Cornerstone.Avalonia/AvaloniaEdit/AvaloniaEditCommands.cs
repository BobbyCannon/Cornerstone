#region References

using Avalonia.Input;
using Cornerstone.Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit;

/// <summary>
/// Custom commands for AvalonEdit.
/// </summary>
public static class AvaloniaEditCommands
{
	#region Constructors

	static AvaloniaEditCommands()
	{
		ConvertLeadingSpacesToTabs = new(nameof(ConvertLeadingSpacesToTabs));
		ConvertLeadingTabsToSpaces = new(nameof(ConvertLeadingTabsToSpaces));
		ConvertSpacesToTabs = new(nameof(ConvertSpacesToTabs));
		ConvertTabsToSpaces = new(nameof(ConvertTabsToSpaces));
		ConvertToLowercase = new(nameof(ConvertToLowercase));
		ConvertToTitleCase = new(nameof(ConvertToTitleCase));
		ConvertToUppercase = new(nameof(ConvertToUppercase));
		DuplicateLine = new(nameof(DuplicateLine), new KeyGesture(Key.D, KeyModifiers.Control));
		IndentSelection = new(nameof(IndentSelection), new KeyGesture(Key.I, KeyModifiers.Control));
		InvertCase = new(nameof(InvertCase));
		RemoveLeadingWhitespace = new(nameof(RemoveLeadingWhitespace));
		RemoveTrailingWhitespace = new(nameof(RemoveTrailingWhitespace));
		ToggleOverstrike = new(nameof(ToggleOverstrike), new KeyGesture(Key.Insert));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Converts leading spaces to tabs in the selected lines (or the whole document if the selection is empty).
	/// </summary>
	public static RoutedCommand ConvertLeadingSpacesToTabs { get; }

	/// <summary>
	/// Converts leading tabs to spaces in the selected lines (or the whole document if the selection is empty).
	/// </summary>
	public static RoutedCommand ConvertLeadingTabsToSpaces { get; }

	/// <summary>
	/// Converts spaces to tabs in the selected text.
	/// </summary>
	public static RoutedCommand ConvertSpacesToTabs { get; }

	/// <summary>
	/// Converts tabs to spaces in the selected text.
	/// </summary>
	public static RoutedCommand ConvertTabsToSpaces { get; }

	/// <summary>
	/// Converts the selected text to lower case.
	/// </summary>
	public static RoutedCommand ConvertToLowercase { get; }

	/// <summary>
	/// Converts the selected text to title case.
	/// </summary>
	public static RoutedCommand ConvertToTitleCase { get; }

	/// <summary>
	/// Converts the selected text to upper case.
	/// </summary>
	public static RoutedCommand ConvertToUppercase { get; }

	/// <summary>
	/// Deletes the current line.
	/// The default shortcut is Ctrl+D.
	/// </summary>
	public static RoutedCommand DuplicateLine { get; }

	/// <summary>
	/// InputModifiers
	/// Runs the IIndentationStrategy on the selected lines (or the whole document if the selection is empty).
	/// </summary>
	public static RoutedCommand IndentSelection { get; }

	/// <summary>
	/// Inverts the case of the selected text.
	/// </summary>
	public static RoutedCommand InvertCase { get; }

	/// <summary>
	/// Removes leading whitespace from the selected lines (or the whole document if the selection is empty).
	/// </summary>
	public static RoutedCommand RemoveLeadingWhitespace { get; }

	/// <summary>
	/// Removes trailing whitespace from the selected lines (or the whole document if the selection is empty).
	/// </summary>
	public static RoutedCommand RemoveTrailingWhitespace { get; }

	/// <summary>
	/// Toggles Overstrike mode
	/// The default shortcut is Ins.
	/// </summary>
	public static RoutedCommand ToggleOverstrike { get; }

	#endregion
}