#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Helpers;

public class QueryStringHelper
{
	#region Fields

	public static string ContentTypeKey = "Content-Type";

	#endregion

	#region Methods

	public static string RemovePossibleQueryString(string url)
	{
		if (string.IsNullOrEmpty(url))
		{
			return string.Empty;
		}

		var indexOfQueryString = url!.IndexOf("?", 0, url.Length, StringComparison.Ordinal);
		return indexOfQueryString == -1 ? url : url.Substring(0, indexOfQueryString);
	}

	#endregion
}