#region References

using System;
using System.Collections.Generic;
using System.Web;

#endregion

namespace Cornerstone.Extensions;

public static class UriExtensions
{
	#region Methods

	public static string[] ToApplicationArguments(this Uri uri)
	{
		var response = new List<string> { "-Schema", uri.Scheme, "-Host", uri.Host };
		if (!string.IsNullOrWhiteSpace(uri.AbsolutePath))
		{
			response.Add("-Path");
			response.Add(uri.AbsolutePath);
		}
		if (!string.IsNullOrWhiteSpace(uri.Query))
		{
			response.Add("-Query");
			response.Add(uri.Query);
			ExpandQuery(uri.Query, response);
		}
		return response.ToArray();
	}

	private static void ExpandQuery(string query, List<string> response)
	{
		var values = HttpUtility.ParseQueryString(query);
		foreach (var key in values.AllKeys)
		{
			response.Add("-" + key);
			response.Add(values[key]);
		}
	}

	#endregion
}