#region References

using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace Cornerstone.Extensions;

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

	public static string[] ToApplicationArguments(this Uri uri)
	{
		var response = new List<string>();
		response.Add("-Schema", uri.Scheme);
		response.Add("-Host", uri.Host);
		if (!string.IsNullOrWhiteSpace(uri.AbsolutePath))
		{
			response.Add("-Path", uri.AbsolutePath);
		}
		if (!string.IsNullOrWhiteSpace(uri.Query))
		{
			response.Add("-Query", uri.Query);
		}
		return response.ToArray();
	}

	#endregion
}