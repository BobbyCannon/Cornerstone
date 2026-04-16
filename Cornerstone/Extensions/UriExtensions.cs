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

	/// <summary>
	/// Update a query parameter.
	/// </summary>
	public static string UpdateQueryParameter(this string uri, string key, string value)
	{
		if (string.IsNullOrEmpty(uri))
		{
			throw new ArgumentException("URI cannot be null or empty", nameof(uri));
		}
		if (!Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out var parsedUri))
		{
			throw new ArgumentException("Invalid URI format", nameof(uri));
		}

		var queryParams = HttpUtility.ParseQueryString(parsedUri.Query ?? string.Empty);
		queryParams[key] = value;

		var builder = new UriBuilder(parsedUri) { Query = queryParams.ToString() };
		return builder.ToString();
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