#region References

using Avalonia.Input;
using Cornerstone.Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Search;

/// <summary>
/// Search commands for AvalonEdit.
/// </summary>
public static class SearchCommands
{
	#region Fields

	/// <summary>
	/// Closes the SearchPanel.
	/// </summary>
	public static readonly RoutedCommand CloseSearchPanel = new(nameof(CloseSearchPanel), new KeyGesture(Key.Escape));

	/// <summary>
	/// Finds the next occurrence in the file.
	/// </summary>
	public static readonly RoutedCommand FindNext = new(nameof(FindNext), new KeyGesture(Key.F3));

	/// <summary>
	/// Finds the previous occurrence in the file.
	/// </summary>
	public static readonly RoutedCommand FindPrevious = new(nameof(FindPrevious), new KeyGesture(Key.F3, KeyModifiers.Shift));

	/// <summary>
	/// Replaces all the occurrences in the document.
	/// </summary>
	public static readonly RoutedCommand ReplaceAll = new(nameof(ReplaceAll), new KeyGesture(Key.A, KeyModifiers.Alt));

	/// <summary>
	/// Replaces the next occurrence in the document.
	/// </summary>
	public static readonly RoutedCommand ReplaceNext = new(nameof(ReplaceNext), new KeyGesture(Key.R, KeyModifiers.Alt));

	#endregion
}