#region References

using System;
using System.IO;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Extensions;

public static class UriExtensions
{
	#region Methods

	public static bool IsBaseOfPage(this Uri baseUri, string uriString)
	{
		if (Path.HasExtension(uriString))
		{
			return false;
		}

		var uri = new Uri(uriString!);
		return baseUri.IsBaseOf(uri);
	}

	#endregion
}