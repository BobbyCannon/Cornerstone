#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Search;

/// <summary>
/// Loose text lookup: whitespace tokens are AND; each token may match any haystack as a substring.
/// Empty or whitespace filter matches everything.
/// </summary>
public static class TokenTextFilter
{
	#region Methods

	public static bool Matches(string filterText, string haystack)
	{
		return MatchesCore(filterText, haystack, null, null);
	}

	public static bool Matches(string filterText, string haystack1, string haystack2)
	{
		return MatchesCore(filterText, haystack1, haystack2, null);
	}

	public static bool Matches(string filterText, string haystack1, string haystack2, string haystack3)
	{
		return MatchesCore(filterText, haystack1, haystack2, haystack3);
	}

	public static bool Matches(string filterText, IReadOnlyList<string> haystacks)
	{
		if (string.IsNullOrWhiteSpace(filterText))
		{
			return true;
		}

		if ((haystacks == null) || (haystacks.Count == 0))
		{
			return false;
		}

		var tokens = filterText.Trim().Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i < tokens.Length; i++)
		{
			var token = tokens[i];
			var found = false;
			for (var h = 0; h < haystacks.Count; h++)
			{
				if (ContainsIgnoreCase(haystacks[h], token))
				{
					found = true;
					break;
				}
			}

			if (!found)
			{
				return false;
			}
		}

		return true;
	}

	private static bool ContainsIgnoreCase(string haystack, string token)
	{
		return (haystack != null)
			&& (haystack.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static bool MatchesCore(string filterText, string haystack1, string haystack2, string haystack3)
	{
		if (string.IsNullOrWhiteSpace(filterText))
		{
			return true;
		}

		var tokens = filterText.Trim().Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i < tokens.Length; i++)
		{
			var token = tokens[i];
			if (!ContainsIgnoreCase(haystack1, token)
				&& !ContainsIgnoreCase(haystack2, token)
				&& !ContainsIgnoreCase(haystack3, token))
			{
				return false;
			}
		}

		return true;
	}

	#endregion
}