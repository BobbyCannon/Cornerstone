#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Search;

/// <summary>
/// EventArgs for <see cref="SearchPanel.SearchOptionsChanged" /> event.
/// </summary>
public class SearchOptionsChangedEventArgs : EventArgs
{
	#region Constructors

	/// <summary>
	/// Creates a new SearchOptionsChangedEventArgs instance.
	/// </summary>
	public SearchOptionsChangedEventArgs(string searchPattern, bool matchCase, bool useRegex, bool wholeWords)
	{
		SearchPattern = searchPattern;
		MatchCase = matchCase;
		UseRegex = useRegex;
		WholeWords = wholeWords;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets whether the search pattern should be interpreted case-sensitive.
	/// </summary>
	public bool MatchCase { get; }

	/// <summary>
	/// Gets the search pattern.
	/// </summary>
	public string SearchPattern { get; }

	/// <summary>
	/// Gets whether the search pattern should be interpreted as regular expression.
	/// </summary>
	public bool UseRegex { get; }

	/// <summary>
	/// Gets whether the search pattern should only match whole words.
	/// </summary>
	public bool WholeWords { get; }

	#endregion
}