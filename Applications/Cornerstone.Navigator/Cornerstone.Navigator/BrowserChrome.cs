#region References

using System;

#endregion

namespace Cornerstone.Navigator;

/// <summary>
/// Status and security copy for the Navigator chrome.
/// </summary>
public static class BrowserChrome
{
	#region Constants

	public const string DefaultPageTitle = "Cornerstone Navigator";
	public const int PageTitleMaxLength = 40;

	#endregion

	#region Methods

	public static string PageTitle(string documentTitle)
	{
		if (string.IsNullOrWhiteSpace(documentTitle))
		{
			return DefaultPageTitle;
		}

		var title = documentTitle.Trim();
		var pipe = title.IndexOf(" | ", StringComparison.Ordinal);
		if (pipe > 0)
		{
			title = title.Substring(0, pipe).Trim();
		}

		if (title.Length <= PageTitleMaxLength)
		{
			return title;
		}

		return title.Substring(0, PageTitleMaxLength).TrimEnd() + "…";
	}

	public static string HostFromUri(string uri)
	{
		if (string.IsNullOrWhiteSpace(uri))
		{
			return string.Empty;
		}

		if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
		{
			return string.Empty;
		}

		return parsed.Host;
	}

	public static bool IsSecure(string uri)
	{
		if (string.IsNullOrWhiteSpace(uri))
		{
			return false;
		}

		if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
		{
			return false;
		}

		return parsed.Scheme == Uri.UriSchemeHttps;
	}

	public static string SecurityText(string uri)
	{
		if (string.IsNullOrWhiteSpace(uri) || !Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
		{
			return string.Empty;
		}

		if (parsed.Scheme == Uri.UriSchemeHttps)
		{
			return "Secure";
		}

		if (parsed.Scheme == Uri.UriSchemeHttp)
		{
			return "Not secure";
		}

		return "Local";
	}

	public static string StatusText(bool isNavigating, string host)
	{
		if (!isNavigating)
		{
			return "Document: Done";
		}

		if (string.IsNullOrWhiteSpace(host))
		{
			return "Connecting...";
		}

		return "Connecting to " + host + "...";
	}

	#endregion
}