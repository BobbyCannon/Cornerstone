#region References

using System;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Search;

/// <summary>
/// Provides factory methods for ISearchStrategies.
/// </summary>
public static class SearchStrategyFactory
{
	#region Methods

	/// <summary>
	/// Creates a default ISearchStrategy with the given parameters.
	/// </summary>
	public static ISearchStrategy Create(string searchPattern, bool ignoreCase, bool matchWholeWords, SearchMode mode)
	{
		if (searchPattern == null)
		{
			throw new ArgumentNullException(nameof(searchPattern));
		}
		var options = RegexOptions.Multiline;
		if (ignoreCase)
		{
			options |= RegexOptions.IgnoreCase;
		}

		switch (mode)
		{
			case SearchMode.Normal:
				searchPattern = Regex.Escape(searchPattern);
				break;
			case SearchMode.Wildcard:
				searchPattern = ConvertWildcardsToRegex(searchPattern);
				break;
		}

		try
		{
			var pattern = new Regex(searchPattern, options);
			return new RegexSearchStrategy(pattern, matchWholeWords);
		}
		catch (ArgumentException ex)
		{
			throw new SearchPatternException(ex.Message, ex);
		}
	}

	private static string ConvertWildcardsToRegex(string searchPattern)
	{
		if (string.IsNullOrEmpty(searchPattern))
		{
			return "";
		}

		var builder = new StringBuilder();

		foreach (var ch in searchPattern)
		{
			switch (ch)
			{
				case '?':
					builder.Append(".");
					break;
				case '*':
					builder.Append(".*");
					break;
				default:
					builder.Append(Regex.Escape(ch.ToString()));
					break;
			}
		}

		return builder.ToString();
	}

	#endregion
}