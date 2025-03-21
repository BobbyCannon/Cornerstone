#region References

using System.Net;
using System.Net.Http.Headers;
using Cornerstone.Convert;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for HTTP (ish)
/// </summary>
public static class HttpExtensions
{
	#region Methods

	/// <summary>
	/// Add or update the value in the HTTP headers collection.
	/// </summary>
	/// <param name="headers"> The headers to be updated. </param>
	/// <param name="key"> The key of the value. </param>
	/// <param name="value"> The value of the entry. </param>
	public static void AddOrUpdate(this HttpHeaders headers, string key, string value)
	{
		if (headers.Contains(key))
		{
			headers.Remove(key);
		}

		headers.Add(key, value);
	}

	/// <summary>
	/// Get the value from the HTTP headers collection or return the provided default.
	/// </summary>
	/// <param name="headers"> The headers to be read from. </param>
	/// <param name="key"> The key of the value. </param>
	/// <param name="defaultValue"> The value to return if key is not in the header. </param>
	/// <returns> The value found otherwise the default value. </returns>
	public static T GetValueOrDefault<T>(this HttpHeaders headers, string key, T defaultValue)
	{
		return headers.TryGetValues(key, out var values)
			? string.Join("", values).ConvertTo<T>()
			: defaultValue;
	}

	/// <summary>
	/// A value that indicates whether the HTTP response was successful.
	/// </summary>
	/// <param name="response"> The response to validate. </param>
	/// <returns>
	/// True if HttpStatusCode is in the successful range (200-299) otherwise false;
	/// </returns>
	public static bool IsSuccessStatusCode(this HttpWebResponse response)
	{
		var asInt = (int) response.StatusCode;
		return (asInt >= 200) && (asInt <= 299);
	}

	#endregion
}