#region References

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#endregion

namespace Cornerstone.VisualStudio.Core.Parsing;

/// <summary>
/// Extracts resource keys from AXAML/XAML text for StaticResource / DynamicResource completion.
/// </summary>
/// <remarks>
/// Cheap and resilient to incomplete markup. Does not build a full resource graph;
/// it only finds <c>x:Key="..."</c> / <c>x:Key='...'</c> attributes in the given text.
/// </remarks>
public static class ResourceKeyScanner
{
	// x:Key="..." or x:Key='...' (allow whitespace around =).
	// Markup-extension keys (containing '{') are skipped at match processing time.
	private static readonly Regex XKeyAttributeRegex = new(
		@"\bx:Key\s*=\s*(?:""(?<key>[^""]*)""|'(?<key>[^']*)')",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	/// <summary>
	/// Returns distinct resource keys found in <paramref name="xaml"/>, in document order.
	/// </summary>
	public static IReadOnlyList<string> FindKeys(string? xaml)
	{
		if (string.IsNullOrEmpty(xaml))
		{
			return [];
		}

		List<string>? keys = null;
		HashSet<string>? seen = null;

		foreach (Match match in XKeyAttributeRegex.Matches(xaml))
		{
			var key = match.Groups["key"].Value;
			if (string.IsNullOrWhiteSpace(key))
			{
				continue;
			}

			// Skip keys that are themselves markup extensions (e.g. x:Key="{x:Type Button}").
			if (key.IndexOf('{') >= 0)
			{
				continue;
			}

			seen ??= new HashSet<string>(StringComparer.Ordinal);
			if (!seen.Add(key))
			{
				continue;
			}

			keys ??= [];
			keys.Add(key);
		}

		return keys is null ? [] : keys;
	}
}
