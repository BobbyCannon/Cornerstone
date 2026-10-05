#region References

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#endregion

namespace Cornerstone.VisualStudio.Core.Parsing;

/// <summary>
/// Finds style class names in Selector="..." values (e.g. Button.ControlCard)
/// so Go To Definition can jump to the Style that defines the class.
/// </summary>
public static class StyleClassScanner
{
	#region Fields

	private static readonly Regex SelectorAttributeRegex = new(
		@"\bSelector\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)')",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex ClassesAttributeRegex = new(
		@"\bClasses\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)')",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex StyleClassInSelectorRegex = new(
		@"\.(?<class>[\w\-]+)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex BracketClauseRegex = new(
		@"\[[^\]]*\]",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	#endregion

	#region Methods

	/// <summary>
	/// Distinct style class names from Selector="..." values (e.g. Button.ControlCard,
	/// ^.Search) and Classes="..." usages, in document order.
	/// Bracket clauses like [(Theme.Color)=None] are ignored so attached properties
	/// are not treated as class names.
	/// </summary>
	public static IReadOnlyList<string> FindClassNames(string xml)
	{
		if (string.IsNullOrEmpty(xml))
		{
			return [];
		}

		List<string> names = null;
		HashSet<string> seen = null;
		foreach (Match attribute in SelectorAttributeRegex.Matches(xml))
		{
			var value = attribute.Groups["value"];
			if (!value.Success)
			{
				continue;
			}

			var selector = BracketClauseRegex.Replace(value.Value, "");
			foreach (Match styleClass in StyleClassInSelectorRegex.Matches(selector))
			{
				AddClassName(styleClass.Groups["class"].Value, ref names, ref seen);
			}
		}

		foreach (Match attribute in ClassesAttributeRegex.Matches(xml))
		{
			var value = attribute.Groups["value"];
			if (!value.Success || string.IsNullOrWhiteSpace(value.Value))
			{
				continue;
			}

			foreach (var token in value.Value.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
			{
				AddClassName(token, ref names, ref seen);
			}
		}

		return names ?? [];
	}

	private static void AddClassName(string name, ref List<string> names, ref HashSet<string> seen)
	{
		if (string.IsNullOrEmpty(name))
		{
			return;
		}

		seen ??= new HashSet<string>(StringComparer.Ordinal);
		if (!seen.Add(name))
		{
			return;
		}

		names ??= [];
		names.Add(name);
	}

	/// <summary>
	/// Offset of the first Selector style class named className that does not contain
	/// excludeOffset (the caret). Returns -1 when none exist besides the current token.
	/// </summary>
	public static int FindSelectorClassOffset(string xml, string className, int excludeOffset)
	{
		if (string.IsNullOrEmpty(xml) || string.IsNullOrEmpty(className))
		{
			return -1;
		}

		foreach (var offset in EnumerateSelectorClassOffsets(xml, className))
		{
			if ((excludeOffset >= offset) && (excludeOffset <= (offset + className.Length)))
			{
				continue;
			}

			return offset;
		}

		return -1;
	}

	public static IEnumerable<int> EnumerateSelectorClassOffsets(string xml, string className)
	{
		if (string.IsNullOrEmpty(xml) || string.IsNullOrEmpty(className))
		{
			yield break;
		}

		foreach (Match attribute in SelectorAttributeRegex.Matches(xml))
		{
			var value = attribute.Groups["value"];
			if (!value.Success)
			{
				continue;
			}

			foreach (Match styleClass in StyleClassInSelectorRegex.Matches(value.Value))
			{
				var name = styleClass.Groups["class"].Value;
				if (!string.Equals(name, className, StringComparison.Ordinal))
				{
					continue;
				}

				yield return value.Index + styleClass.Groups["class"].Index;
			}
		}
	}

	#endregion
}
