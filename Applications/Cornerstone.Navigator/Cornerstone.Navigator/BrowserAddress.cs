#region References

using System;

#endregion

namespace Cornerstone.Navigator;

/// <summary>
/// Resolves address-bar input to a navigation URI (no live DNS).
/// </summary>
public static class BrowserAddress
{
	#region Constants

	public const string HomeUri = "https://github.com/BobbyCannon/Cornerstone";
	public const string SearchPrefix = "https://www.bing.com/search?q=";

	#endregion

	#region Methods

	public static string Resolve(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return HomeUri;
		}

		input = input.Trim();

		if (Uri.TryCreate(input, UriKind.Absolute, out var absolute)
			&& ((absolute.Scheme == Uri.UriSchemeHttp)
				|| (absolute.Scheme == Uri.UriSchemeHttps)
				|| (absolute.Scheme == "about")))
		{
			return absolute.OriginalString;
		}

		if (LooksLikeHost(input) && Uri.TryCreate("https://" + input, UriKind.Absolute, out var https))
		{
			return https.OriginalString;
		}

		return SearchPrefix + Uri.EscapeDataString(input);
	}

	private static bool LooksLikeHost(string input)
	{
		if (input.Contains(' ') || input.Contains('\\'))
		{
			return false;
		}

		if (!input.Contains('.'))
		{
			return false;
		}

		if (input.StartsWith('.') || input.EndsWith('.'))
		{
			return false;
		}

		return true;
	}

	#endregion
}